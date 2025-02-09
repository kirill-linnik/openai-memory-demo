namespace Backend.Models.Files;

public readonly record struct PageDetail(
    int Index,
    int Offset,
    string Text);
