using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace MarioPicrossLoader3000 {
	internal class Program {
		const int LevelSize = 0x20;
		const int LevelsStartOffset = 0x92b0;
		const int LevelsEndOffset = 0xa2b0;
		public static List<Level> levels = new List<Level>();
		static void Main(string[] args) {
			string romPath = @"C:\Users\user\Downloads\Mario's Picross (UE) [S][!].gb";

			byte[] romData = File.ReadAllBytes(romPath);

			for(int offset = LevelsStartOffset; offset < LevelsEndOffset; offset += LevelSize) {
				Level level = DecodeLevel(romData, offset);
				//PrintLevel(level);
				levels.Add(level);
			}


			StringBuilder htmlTemplate = new StringBuilder();
			htmlTemplate.Append("<html><head><style>");
			htmlTemplate.Append("table { border-collapse: collapse; page-break-inside: avoid; }");
			htmlTemplate.Append("th, td { border: 1px solid black; }");
			htmlTemplate.Append("tr { page-break-inside: avoid; }");
			htmlTemplate.Append("td, th { width: 40px; height: 40px; max-width: 40px; max-height: 40px; margin: 0px }");
			htmlTemplate.Append("td, th { white-space: nowrap; }");

			htmlTemplate.Append("</style></head><body>");

			StringBuilder htmlRevealed = new StringBuilder(htmlTemplate.ToString());

			for(int i = 0; i < levels.Count; i++) {
				htmlRevealed.Append($"<h1>Level {i + 1}</h1>");
				htmlRevealed.Append(levels[i].GenerateHtmlTableWithAnswers());
			}

			htmlTemplate.Append("</body></html>");
			File.WriteAllText("levels_revealed.html", htmlRevealed.ToString());

			StringBuilder stringBuilder = new StringBuilder(htmlTemplate.ToString());
			for(int i = 0; i < levels.Count; i++) {
				htmlRevealed.Append($"<h1>Level {i + 1}</h1>");
				Level level = levels[i];
				var usedRows = level.GetUsedRows();
				var usedCols = level.GetUsedCols();
				int randomRow = usedRows.Count > 0 ? usedRows[new Random().Next(usedRows.Count)] : -1;
				int randomCol = usedCols.Count > 0 ? usedCols[new Random().Next(usedCols.Count)] : -1;


				if(new Random().Next(101) < 50) {
					stringBuilder.Append(level.GenerateHtmlTableWithPartialReveal(randomRow, -1));
				}
				else {
					stringBuilder.Append(level.GenerateHtmlTableWithPartialReveal(-1, randomCol));
				}
			}
			stringBuilder.Append("</body></html>");
			File.WriteAllText("levels_partial.html", stringBuilder.ToString());

			StringBuilder htmlEmpty = new StringBuilder(htmlTemplate.ToString());

			for(int i = 0; i < levels.Count; i++) {
				htmlEmpty.Append($"<h1>Level {i + 1}</h1>");
				htmlEmpty.Append(levels[i].GenerateHtmlTable());
			}

			htmlEmpty.Append("</body></html>");
			File.WriteAllText("levels_empty.html", htmlEmpty.ToString());
		}

		static Level DecodeLevel(byte[] romData, int offset) {
			int width = romData[offset + 0x1E];
			int height = romData[offset + 0x1F];

			Level level = new Level {
				LevelOffset = offset,
				LevelSize = width
			};

			for(int i = 0; i < height; i++) {
				byte byte1 = romData[offset + i * 2];
				byte byte2 = romData[offset + i * 2 + 1];

				for(int j = 0; j < 8; j++) {
					level.Grid[i, j] = (byte1 & (1 << (7 - j))) != 0;
				}
				for(int j = 0; j < 8; j++) {
					level.Grid[i, j + 8] = (byte2 & (1 << (7 - j))) != 0;
				}
			}

			return level;
		}

		static void PrintLevel(Level level) {
			Console.WriteLine($"Level at offset 0x{level.LevelOffset:X} (Dimensions: {level.LevelSize}x{level.LevelSize})");

			for(int i = 0; i < level.LevelSize; i++) {
				for(int j = 0; j < level.LevelSize; j++) {
					Console.Write(level.Grid[i, j] ? '#' : ' ');
				}
				Console.WriteLine();
			}

			Console.WriteLine();
		}

		public class Level {
			public int LevelOffset { get; set; }
			public int LevelNumber { get; set; }
			public int LevelSize { get; set; } // 5, 10, 15 or 20
			public bool[,] Grid = new bool[20, 20];
			public Level() {

			}
			public List<int> GetNumberOfRevealedInColumn(int columnIndex) {
				//return the specified number of filled cells in the column, and when there is space between filled cells, it gets split into multiple numbers
				//for example, if there is 10 cells tall column, filled with 0111001101 (where 0 is empty cell and 1 is filled cell), it should return [3, 2, 1]

				List<int> revealedCounts = new List<int>();
				int count = 0;

				for(int row = 0; row < LevelSize; row++) {
					if(Grid[row, columnIndex]) {
						count++;
					}
					else {
						if(count > 0) {
							revealedCounts.Add(count);
							count = 0;
						}
					}
				}
				if(count > 0) {
					revealedCounts.Add(count);
				}

				return revealedCounts;
			}
			public List<int> GetNumberOfRevealedInRow(int rowIndex) {
				List<int> revealedCounts = new List<int>();
				int count = 0;

				for(int col = 0; col < LevelSize; col++) {
					if(Grid[rowIndex, col]) {
						count++;
					}
					else {
						if(count > 0) {
							revealedCounts.Add(count);
							count = 0;
						}
					}
				}

				if(count > 0) {
					revealedCounts.Add(count);
				}

				return revealedCounts;
			}

			//returns list of indexes rows that have at least one filled cell
			public List<int> GetUsedRows() {
				List<int> usedRows = new List<int>();
				for(int row = 0; row < LevelSize; row++) {
					for(int col = 0; col < LevelSize; col++) {
						if(Grid[row, col]) {
							usedRows.Add(row);
							break;
						}
					}
				}
				return usedRows;
			}
			//returns list of indexes of columns that have at least one filled cell
			public List<int> GetUsedCols() {
				List<int> usedCols = new List<int>();
				for(int col = 0; col < LevelSize; col++) {
					for(int row = 0; row < LevelSize; row++) {
						if(Grid[row, col]) {
							usedCols.Add(col);
							break;
						}
					}
				}
				return usedCols;
			}
			public string GenerateHtmlTable() {
				var html = new System.Text.StringBuilder();
				html.Append("<table border='1'>");

				//header column
				html.Append("<tr><th></th>");
				for(int col = 0; col < LevelSize; col++) {
					var revealedCounts = GetNumberOfRevealedInColumn(col);
					html.Append("<th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
				}
				html.Append("</tr>");

				//header row
				for(int row = 0; row < LevelSize; row++) {
					var revealedCounts = GetNumberOfRevealedInRow(row);
					html.Append("<tr><th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
					for(int col = 0; col < LevelSize; col++) {
						html.Append("<td style='width:20px;height:20px;background-color:#ccc;'></td>");
					}
					html.Append("</tr>");
				}

				html.Append("</table>");
				return html.ToString();
			}

			public string GenerateHtmlTableWithAnswers() {
				var html = new System.Text.StringBuilder();
				html.Append("<table border='1'>");

				//header column
				html.Append("<tr><th></th>");
				for(int col = 0; col < LevelSize; col++) {
					var revealedCounts = GetNumberOfRevealedInColumn(col);
					html.Append("<th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
				}
				html.Append("</tr>");

				//header row
				for(int row = 0; row < LevelSize; row++) {
					var revealedCounts = GetNumberOfRevealedInRow(row);
					html.Append("<tr><th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
					for(int col = 0; col < LevelSize; col++) {
						string cellColor = Grid[row, col] ? "#000" : "#fff";
						html.Append($"<td style='width:20px;height:20px;background-color:{cellColor};'></td>");
					}
					html.Append("</tr>");
				}

				html.Append("</table>");
				return html.ToString();
			}

			public string GenerateHtmlTableWithPartialReveal(int rowReveal, int columnReveal) {
				var html = new System.Text.StringBuilder();
				html.Append("<table border='1'>");

				//header column
				html.Append("<tr><th></th>");
				for(int col = 0; col < LevelSize; col++) {
					var revealedCounts = GetNumberOfRevealedInColumn(col);
					html.Append("<th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
				}
				html.Append("</tr>");

				//header row
				for(int row = 0; row < LevelSize; row++) {
					var revealedCounts = GetNumberOfRevealedInRow(row);
					html.Append("<tr><th>");
					foreach(var count in revealedCounts) {
						html.Append($"{count},");
					}
					html.Append("</th>");
					for(int col = 0; col < LevelSize; col++) {
						bool revealCell = (row == rowReveal || col == columnReveal || (rowReveal == -1 && col == columnReveal) || (columnReveal == -1 && row == rowReveal));
						string cellColor = revealCell ? (Grid[row, col] ? "#000" : "#fff") : "#ccc";
						html.Append($"<td style='width:20px;height:20px;background-color:{cellColor};'></td>");
					}
					html.Append("</tr>");
				}

				html.Append("</table>");
				return html.ToString();
			}
		}
	}
}
