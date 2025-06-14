// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Aspire.SK.RAG.Models;

public struct AIChatFile
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("data")]
    public BinaryData Data { get; set; }
}
