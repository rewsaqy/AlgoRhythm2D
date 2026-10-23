namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Word-wrap murni-C# untuk label tutorial dunia (TextMesh).
    /// Tidak ada dependensi Unity agar bisa diuji di EditMode.
    /// Aturan: bungkus per kata, hormati baris baru eksplisit,
    /// potong kata yang lebih panjang dari batas, dan batasi
    /// jumlah baris agar teks selalu muat di layar.
    /// </summary>
    public static class DialogueLayout
    {
        /// <summary>Bungkus teks ke beberapa baris dengan batas kolom.</summary>
        public static string Wrap(string text, int maxCharsPerLine, int maxLines)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (maxCharsPerLine < 8)
            {
                maxCharsPerLine = 8;
            }

            if (maxLines < 1)
            {
                maxLines = 1;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = normalized.Split('\n');
            var lines = new System.Collections.Generic.List<string>(maxLines);

            foreach (string paragraph in paragraphs)
            {
                string[] words = paragraph.Split(
                    new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    lines.Add(string.Empty);
                    if (lines.Count >= maxLines)
                    {
                        break;
                    }

                    continue;
                }

                string current = string.Empty;
                foreach (string rawWord in words)
                {
                    string word = rawWord;
                    while (word.Length > maxCharsPerLine)
                    {
                        if (current.Length > 0)
                        {
                            lines.Add(current);
                            current = string.Empty;
                            if (lines.Count >= maxLines)
                            {
                                break;
                            }
                        }

                        lines.Add(word.Substring(0, maxCharsPerLine));
                        word = word.Substring(maxCharsPerLine);
                        if (lines.Count >= maxLines)
                        {
                            break;
                        }
                    }

                    if (lines.Count >= maxLines)
                    {
                        break;
                    }

                    if (word.Length == 0)
                    {
                        continue;
                    }

                    if (current.Length == 0)
                    {
                        current = word;
                    }
                    else if (current.Length + 1 + word.Length <= maxCharsPerLine)
                    {
                        current = current + " " + word;
                    }
                    else
                    {
                        lines.Add(current);
                        if (lines.Count >= maxLines)
                        {
                            current = string.Empty;
                            break;
                        }

                        current = word;
                    }
                }

                if (lines.Count >= maxLines)
                {
                    break;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                    if (lines.Count >= maxLines)
                    {
                        break;
                    }
                }
            }

            while (lines.Count > maxLines)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines.ToArray());
        }

        /// <summary>
        /// Bungkus badan dialog dengan prefix pembicara (misal "A.I.D.A : ")
        /// yang menempel di baris pertama. Prefix tidak pernah dipisah.
        /// </summary>
        public static string WrapBody(string speakerPrefix, string body, int maxCharsPerLine, int maxLines)
        {
            if (string.IsNullOrEmpty(body))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(speakerPrefix))
            {
                return Wrap(body, maxCharsPerLine, maxLines);
            }

            if (maxCharsPerLine < 8)
            {
                maxCharsPerLine = 8;
            }

            if (maxLines < 1)
            {
                maxLines = 1;
            }

            int firstBudget = maxCharsPerLine - speakerPrefix.Length;
            if (firstBudget < 8)
            {
                firstBudget = 8;
            }

            string normalized = body.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = normalized.Split('\n');
            var lines = new System.Collections.Generic.List<string>(maxLines);

            foreach (string paragraph in paragraphs)
            {
                string[] words = paragraph.Split(
                    new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    lines.Add(string.Empty);
                    if (lines.Count >= maxLines)
                    {
                        break;
                    }

                    continue;
                }

                string current = string.Empty;
                int budget = lines.Count == 0 ? firstBudget : maxCharsPerLine;
                foreach (string rawWord in words)
                {
                    string word = rawWord;
                    while (word.Length > maxCharsPerLine)
                    {
                        if (current.Length > 0)
                        {
                            lines.Add(current);
                            current = string.Empty;
                            if (lines.Count >= maxLines)
                            {
                                break;
                            }

                            budget = lines.Count == 0 ? firstBudget : maxCharsPerLine;
                        }

                        lines.Add(word.Substring(0, maxCharsPerLine));
                        word = word.Substring(maxCharsPerLine);
                        if (lines.Count >= maxLines)
                        {
                            break;
                        }

                        budget = lines.Count == 0 ? firstBudget : maxCharsPerLine;
                    }

                    if (lines.Count >= maxLines)
                    {
                        break;
                    }

                    if (word.Length == 0)
                    {
                        continue;
                    }

                    if (current.Length == 0)
                    {
                        current = word;
                    }
                    else if (current.Length + 1 + word.Length <= budget)
                    {
                        current = current + " " + word;
                    }
                    else
                    {
                        lines.Add(current);
                        if (lines.Count >= maxLines)
                        {
                            current = string.Empty;
                            break;
                        }

                        budget = lines.Count == 0 ? firstBudget : maxCharsPerLine;
                        current = word;
                    }
                }

                if (lines.Count >= maxLines)
                {
                    break;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                    if (lines.Count >= maxLines)
                    {
                        break;
                    }
                }
            }

            while (lines.Count > maxLines)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines.ToArray());
        }
    }
}
