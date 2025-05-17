using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prefect;

//TODO: Redo this to check the whole template is correct minus a few configurable parameters
internal sealed class DocfxConfigurationRule : FileExistsRule
{
    public override string Description => $"'{RelativePath}' meets the modern Docfx configuration standards";

    public override bool AllowFixupOverwrite => false;

    public new const string RelativePath = "docs/docfx.json";

    public DocfxConfigurationRule(FileInfo referenceFile)
        : base(RelativePath, referenceFile)
    { }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (base.Validate(repo, fullFilePath, relativeFilePath) is string failReason)
            return failReason;

        using Stream stream = File.OpenRead(fullFilePath);

        JsonDocumentOptions documentOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };
        JsonNode? json = JsonNode.Parse(stream, documentOptions: documentOptions);
        if (json is null)
            return $"'{relativeFilePath}' does not contain docfx configuration.";

        StringBuilder errors = new();
        void EmitError(string message)
            => errors.Append($"\n    {message}");

        const string docsOutputRoot = "../artifacts/docs/";

        CheckValue(["build", "output"], $"{docsOutputRoot}site/");
        CheckValue(["build", "globalMetadata", "_appFooter"], "&copy; Bonsai Foundation CIC and Contributors. Made with <a href=\"https://dotnet.github.io/docfx\">docfx</a>");

        if (TryGetValue(json, ["build", "dest"], out _))
            EmitError("'build.dest' is deprecated.");

        if (TryGetNode(json, ["metadata"], out JsonArray? metadataDefinitions))
        {
            for (int i = 0; i < metadataDefinitions.Count; i++)
            {
                JsonNode? metadataNode = metadataDefinitions[i];
                if (TryGetValue(metadataNode, ["dest"], out _))
                    EmitError("'metadata.*.dest' is deprecated");

                if (!TryGetValue(metadataNode, ["output"], out string? output) || !output.StartsWith(docsOutputRoot))
                    EmitError($"'metadata.*.output' must begin with '{docsOutputRoot}'");
            }
        }

        return errors.Length > 0 ? $"'{relativeFilePath}' does not meet the standard for modern .NET projects:{errors}" : null;

        void CheckValue(ReadOnlySpan<string> keyPath, string? expectedValue)
        {
            if (!TryGetValue(json, keyPath, out string? value))
                EmitError($"'{MakeJsonPath(keyPath)}' is missing.");
            else if (value != expectedValue)
                EmitError($"'{MakeJsonPath(keyPath)}' must be {(expectedValue is null ? "null" : $"'{expectedValue}'")}.");
        }

        bool TryGetValue(JsonNode? startNode, ReadOnlySpan<string> keyPath, [NotNullWhen(true)] out string? value)
        {
            value = null;

            if (!TryGetNode(startNode, keyPath, out JsonValue? node))
                return false;

            if (node.GetValueKind() != JsonValueKind.String)
                return false;

            value = node.GetValue<string>();
            return true;
        }

        bool TryGetNode<TNode>(JsonNode? startNode, ReadOnlySpan<string> keyPath, [NotNullWhen(true)] out TNode? result)
            where TNode : JsonNode
        {
            JsonNode? node = startNode;
            result = null;

            while (keyPath.Length > 0)
            {
                if (node is not JsonObject jsonObject)
                    return false;

                if (!jsonObject.TryGetPropertyValue(keyPath[0], out node))
                    return false;

                if (node is null)
                    return false;

                keyPath = keyPath.Slice(1);
            }

            if (node is TNode resultNode)
            {
                result = resultNode;
                return true;
            }

            return false;
        }

        string MakeJsonPath(ReadOnlySpan<string> parts, string? finalPart = null)
        {
            string result = "";
            foreach (string part in parts)
                result += $".{part}";

            if (finalPart is not null)
                result += $".{finalPart}";

            return result.Length == 0 ? "." : result;
        }
    }
}
