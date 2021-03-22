using System;
using DiffPlex;
using System.Collections.Generic;

namespace FantomasVs
{
    public class AgnosticChunker : IChunker
    {
        /// <summary>
        /// Gets the default singleton instance of the chunker.
        /// </summary>
        public static AgnosticChunker Instance { get; } = new AgnosticChunker();

        public string[] Chunk(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            var output = new List<string>(128);

            int lastPosition = 0, currentPosition = 0;

            while (currentPosition < text.Length)
            {
                char ch = text[currentPosition];
                switch (ch)
                {
                    case '\n':
                    case '\r':
                        {

                            if (ch == '\r' && currentPosition < text.Length && text[currentPosition + 1] == '\n')
                                currentPosition += 2;
                            else
                                currentPosition += 1;

                            var str = text.Substring(lastPosition, currentPosition - lastPosition);
                            output.Add(str);

                            lastPosition = currentPosition;
                            break;
                        }

                    default:
                        {
                            currentPosition += 1;
                            break;
                        }
                }
            }

            if (lastPosition != text.Length)
            {
                var str = text.Substring(lastPosition, text.Length - lastPosition);
                output.Add(str);
            }

            return output.ToArray();
        }
    }
}

