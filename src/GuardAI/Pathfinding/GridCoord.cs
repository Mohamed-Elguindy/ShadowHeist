namespace ShadowHeist.GuardAI.Pathfinding;

/// <summary>
/// Immutable 2D grid coordinate (Column, Row) for tile-based pathfinding (Ticket #14 — Hatem).
/// </summary>
public readonly record struct GridCoord(int Col, int Row);

