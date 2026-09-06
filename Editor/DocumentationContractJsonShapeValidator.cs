using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Geurts.GameForge.Documentation
{
    internal static class DocumentationContractJsonShapeValidator
    {
        internal static void Validate(string json)
        {
            new Parser(json).ValidateContract();
        }

        private sealed class Parser
        {
            private readonly string json;
            private int position;

            internal Parser(string json)
            {
                this.json = json ?? throw new ArgumentNullException(nameof(json));
            }

            internal void ValidateContract()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["schemaVersion"] = ReadStringValue,
                    ["packageVersion"] = ReadStringValue,
                    ["source"] = ReadSource,
                    ["destination"] = ReadDestination,
                    ["validationEntries"] = () => ReadArray(ReadStringValue),
                    ["updateUi"] = ReadUpdateUi,
                    ["routeMappings"] = () => ReadArray(ReadRouteMapping)
                });
                SkipWhitespace();
                if (position != json.Length)
                {
                    Fail("Unexpected content follows the root object.");
                }
            }

            private void ReadSource()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["repository"] = ReadStringValue,
                    ["branch"] = ReadStringValue,
                    ["selection"] = ReadStringValue
                });
            }

            private void ReadDestination()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["projectRelativePath"] = ReadStringValue,
                    ["replacement"] = ReadStringValue,
                    ["access"] = ReadStringValue
                });
            }

            private void ReadUpdateUi()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["actionLabel"] = ReadStringValue,
                    ["confirmationDefault"] = ReadStringValue,
                    ["cancelResult"] = ReadStringValue,
                    ["confirmationTargets"] = () => ReadArray(ReadConfirmationTarget)
                });
            }

            private void ReadConfirmationTarget()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["path"] = ReadStringValue,
                    ["effect"] = ReadStringValue
                });
            }

            private void ReadRouteMapping()
            {
                ReadObject(new Dictionary<string, Action>(StringComparer.Ordinal)
                {
                    ["template"] = ReadStringValue,
                    ["target"] = ReadStringValue
                });
            }

            private void ReadObject(IReadOnlyDictionary<string, Action> fields)
            {
                Expect('{');
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    Fail("A required contract object is empty.");
                }

                while (true)
                {
                    string name = ReadString();
                    if (!fields.TryGetValue(name, out Action readValue))
                    {
                        Fail("Unknown contract field: " + name + ".");
                    }

                    if (!seen.Add(name))
                    {
                        Fail("Duplicate contract field: " + name + ".");
                    }

                    Expect(':');
                    readValue();
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Expect(',');
                }

                if (seen.Count != fields.Count)
                {
                    foreach (string field in fields.Keys)
                    {
                        if (!seen.Contains(field))
                        {
                            Fail("Missing contract field: " + field + ".");
                        }
                    }
                }
            }

            private void ReadArray(Action readElement)
            {
                Expect('[');
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return;
                }

                while (true)
                {
                    readElement();
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return;
                    }

                    Expect(',');
                }
            }

            private void ReadStringValue()
            {
                ReadString();
            }

            private string ReadString()
            {
                SkipWhitespace();
                ExpectRaw('"');
                StringBuilder result = new StringBuilder();
                while (position < json.Length)
                {
                    char character = json[position++];
                    if (character == '"')
                    {
                        return result.ToString();
                    }

                    if (character < 0x20)
                    {
                        Fail("A JSON string contains an invalid control character.");
                    }

                    if (character != '\\')
                    {
                        result.Append(character);
                        continue;
                    }

                    if (position >= json.Length)
                    {
                        Fail("A JSON string ends with an incomplete escape.");
                    }

                    char escaped = json[position++];
                    switch (escaped)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ReadUnicodeEscape()); break;
                        default: Fail("A JSON string contains an unsupported escape."); break;
                    }
                }

                Fail("A JSON string is not terminated.");
                return null;
            }

            private char ReadUnicodeEscape()
            {
                if (position + 4 > json.Length)
                {
                    Fail("A JSON Unicode escape is incomplete.");
                }

                int value = 0;
                for (int index = 0; index < 4; index++)
                {
                    char character = json[position++];
                    int digit;
                    if (character >= '0' && character <= '9') digit = character - '0';
                    else if (character >= 'a' && character <= 'f') digit = character - 'a' + 10;
                    else if (character >= 'A' && character <= 'F') digit = character - 'A' + 10;
                    else
                    {
                        Fail("A JSON Unicode escape contains a non-hexadecimal character.");
                        digit = 0;
                    }

                    value = (value * 16) + digit;
                }

                return (char)value;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                ExpectRaw(expected);
            }

            private void ExpectRaw(char expected)
            {
                if (position >= json.Length || json[position] != expected)
                {
                    Fail("Expected '" + expected + "'.");
                }

                position++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (position >= json.Length || json[position] != expected)
                {
                    return false;
                }

                position++;
                return true;
            }

            private void SkipWhitespace()
            {
                while (position < json.Length && char.IsWhiteSpace(json[position]))
                {
                    position++;
                }
            }

            private void Fail(string message)
            {
                throw new InvalidDataException(
                    "The documentation companion contract is not valid schema 1.0.0 JSON at character " +
                    position + ": " + message);
            }
        }
    }
}
