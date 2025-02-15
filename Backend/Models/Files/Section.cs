﻿namespace Backend.Models.Files;

public readonly record struct Section(
    string Id,
    string Content,
    string SourcePage,
    string SourceFile,
    string? Category = null);