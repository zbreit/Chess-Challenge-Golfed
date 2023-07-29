using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections.Immutable;

public class MyBot : IChessBot
{
    /// <summary>
    /// This array mimics the ordering of the <see cref="PieceType" /> enum values (with an extra
    /// "None" value at 0).
    /// </summary>
    public static readonly int[] PieceValues = new[] { 0, 1, 3, 3, 5, 9 };
    ISet<ulong> uniqueBoards = new HashSet<ulong>();
    ulong leafNodesSeen;

    public Move Think(Board board, Timer timer)
    {
        Dictionary<ulong, int> transpositionTable = new();

        uniqueBoards.Clear();
        var response = SearchWithTranspositions(board, transpositionTable);

        Console.WriteLine($"Making move {response.bestMove} w/ evaluation {response.evaluation}.");

        Console.WriteLine($"Seen {leafNodesSeen} boards, {uniqueBoards.Count} ({(float)uniqueBoards.Count / leafNodesSeen:P2}) were unique.");

        return response.bestMove;
    }

    public (Move bestMove, int evaluation) Search(Board board, int depth = 5)
    {
        Move? bestMove = null;
        int bestEvaluation = int.MinValue;

        if (depth == 0)
        {
            return (new(), EvaluateByPieceWeights(board));
        }

        foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);

            int eval = -Search(board, depth - 1).evaluation;

            // Profiling code: TODO: remove!
            uniqueBoards.Add(board.ZobristKey);
            if (depth <= 1)
            {
                leafNodesSeen++;
            }

            if (eval > bestEvaluation)
            {
                bestEvaluation = eval;
                bestMove = move;
            }

            board.UndoMove(move);
        }

        return (bestMove ?? new(), bestEvaluation);
    }


    public (Move bestMove, int evaluation) SearchWithTranspositions(Board board,
        Dictionary<ulong, int> transpositionTable, int depth = 6)
    {
        Move? bestMove = null;
        int bestEvaluation = int.MinValue;

        if (depth == 0)
        {
            return (new(), EvaluateByPieceWeights(board));
        }

        foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);

            if (!transpositionTable.TryGetValue(board.ZobristKey, out int eval))
            {
                eval = -SearchWithTranspositions(board, transpositionTable, depth - 1).evaluation;
            }

            transpositionTable[board.ZobristKey] = eval;

            if (eval > bestEvaluation)
            {
                bestEvaluation = eval;
                bestMove = move;
            }

            board.UndoMove(move);
        }

        return (bestMove ?? new(), bestEvaluation);
    }


    private static int EvaluateByPieceWeights(Board board)
    {
        int whiteEval = WeightedPieceSum(board, isWhite: true);
        int blackEval = WeightedPieceSum(board, isWhite: false);

        return board.IsWhiteToMove ? (whiteEval - blackEval) : (blackEval - whiteEval);
    }

    private static int WeightedPieceSum(Board board, bool isWhite)
    {
        int sum = 0;
        for (int i = 1; i < PieceValues.Length; i++)
        {
            sum += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard((PieceType)i, isWhite)) * PieceValues[i];
        }
        return sum;
    }

    // B: 1010, W: 0100, A: 1110, E(B): +1, E(W): -1
}