using System;
using System.Collections.Generic;
using System.IO;

namespace PngDataExportFromFile
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			foreach (var filePath in args)
			{
				if (!File.Exists(filePath))
				{
					continue;
				}

				var directory = Directory.GetParent(filePath);

				using (var fileStream = File.OpenRead(filePath))
				{
					Span<byte> buffer = new byte[fileStream.Length];
					fileStream.Read(buffer);
					ReadOnlySpan<byte> readOnlyBuffer = buffer;

					var p = 0;
					var pngData = new List<(int start, int length)>();

					while (p < readOnlyBuffer.Length -7)
					{
						if (readOnlyBuffer[p] == 0x89 && readOnlyBuffer[p + 1] == 0x50 && readOnlyBuffer[p + 2] == 0x4e && readOnlyBuffer[p + 3] == 0x47 && readOnlyBuffer[p + 4] == 0x0d && readOnlyBuffer[p + 5] == 0x0a && readOnlyBuffer[p + 6] == 0x1a && readOnlyBuffer[p + 7] == 0x0a)
						{
							var start = p;
							(bool isOK, int length) range = (false, 0);

							//ヘッダー8バイト、IHDR25バイトとIDAT最小の12バイトはとりあえず飛ばす
							var p4End = p + 8 + 25 + 12;

							while (p4End < readOnlyBuffer.Length)
							{
								if (readOnlyBuffer[p4End] == 0x49 && readOnlyBuffer[p4End + 1] == 0x45 && readOnlyBuffer[p4End + 2] == 0x4e && readOnlyBuffer[p4End + 3] == 0x44)
								{
									range = (true, p4End + 7 - start + 1);
									break;
								}

								p4End++;
							}

							if (range.isOK)
							{
								pngData.Add((start, range.length));
								p = p + range.length;
							}
							else
							{
								Console.WriteLine($"{Path.GetFileNameWithoutExtension(filePath)} is broken.");
								break;//というかデータ壊れているのをどうするべきか
							}
						}
						else
						{
							p = p + 1;
						}
					}

					var counter = 1;

					var prefix = "{0:D" + (1 + (pngData.Count / 10)) + "}";

					foreach (var (s, l) in pngData)
					{
						var pngBytes = readOnlyBuffer.Slice(s, l).ToArray();
						File.WriteAllBytes(Path.Combine(directory.FullName, $"{Path.GetFileNameWithoutExtension(filePath)}_{string.Format(prefix, counter)}.png"), pngBytes);
						counter++;
					}

					fileStream.Close();
				}
			}
		}
	}
}
