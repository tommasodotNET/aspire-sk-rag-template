// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Aspire.SK.RAG.Models;

public record AIChatRequest([property: JsonPropertyName("messages")] IList<AIChatMessage> Messages)
{
    [JsonInclude, JsonPropertyName("sessionState")]
    public string? SessionState;

    [JsonInclude, JsonPropertyName("context")]
    public BinaryData? Context;
}
