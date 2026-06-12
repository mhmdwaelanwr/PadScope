namespace PadScope.Core.Testing;

public sealed record TestStageDefinition(
    TestStage Stage,
    string Name,
    string Status,
    string Goal,
    string WhatToDo,
    string PassCriteria
);
