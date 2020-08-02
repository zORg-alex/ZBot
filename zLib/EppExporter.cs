using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zLib {
	public static class EppExporter {
		public static List<Column> Parse(string Path) {
			var l = new List<Column>();

			try {
				FileInfo file = new FileInfo(Path);
				ExcelPackage Ep = new ExcelPackage(file);
				var ws = Ep.Workbook.Worksheets.First();
				var cmax = ws.Dimension.End.Column; var rmax = ws.Dimension.End.Row;
				for (int cn = 1; cn <= cmax; cn++) {
					var c = new Column() { Name = ws.Cells[1, cn].Value.ToString() };
					var values = ws.Cells[2, cn, ws.Dimension.End.Row, cn].ToArray();
					c.ColumnType = values.FirstOrDefault().Value.GetType();
					c.Values = values.Select(v => v.Value).ToList();
					l.Add(c);
				}
			} catch {

			}

			return l;
		}
		public static DataTable ParseToDataTable(string Path) {
			var dtt = new DataTable();

			try {
				FileInfo file = new FileInfo(Path);
				ExcelPackage Ep = new ExcelPackage(file);
				var ws = Ep.Workbook.Worksheets.First();
				var cmax = ws.Dimension.End.Column; var rmax = ws.Dimension.End.Row;
				for (int cn = 1; cn <= cmax; cn++) {
					dtt.Columns.Add(new DataColumn(ws.Cells[1, cn].Value.ToString(), typeof(string)));
				}
				for (int rn = 2; rn <= rmax; rn++) {
					var r = dtt.NewRow();
					for (int cn = 1; cn <= cmax; cn++) {
						if (ws.Cells[rn, cn].Value is object o)
							r[cn - 1] = o.ToString();
					}
					dtt.Rows.Add(r);
				}
			} catch (Exception){ }

			return dtt;
		}
		public class Page {
			public string Name;
			public List<Table> Tables= new List<Table>();

			public static List<Page> GetPages(Table Table, string PageName) {
				return new List<Page>() { new Page() { Name = PageName, Tables = new List<Table>() { Table } } };
			}
			public static List<Page> GetPages(List<Column> Columns, string PageName) {
				return new List<Page>() { new Page() { Name = PageName, Tables = new List<Table>() { new Table() { Columns = Columns } } } };
			}
		}
		public class Table {
			private int posx = 1;
			public int PosX { get { return posx; } set { posx = Math.Max(1, value); } }

			private int posy = 1;
			public int PosY { get { return posy; } set { posy = Math.Max(1, value); } }
			public int ColPosY { get { return posy + ((Name != null) ? 1 : 0); } }

			public string Name;
			public List<Column> Columns;
		}
		public class Column {
			public string Name;
			public List<object> Values;
			public Type ColumnType;
			public int Digits = 2;
		}

		public static void WriteToFile(List<Page> pages, string FullPath) {
			ExcelPackage Ep = new ExcelPackage();
			foreach (var page in pages) {
				Ep.Workbook.Worksheets.Add(page.Name);
				var ws = Ep.Workbook.Worksheets.LastOrDefault();
				foreach (var table in page.Tables) {
					if (table.Name != null) {
						ws.Cells[table.PosY, table.PosX, table.PosY, table.PosX + table.Columns.Count - 1].Merge = true;
						ws.Cells[table.PosY, table.PosX].Value = table.Name;
					}
					if (table.Columns.Sum(c => c.Values.Count()) > 0) {
						foreach (var c in table.Columns) {
							ws.Cells[table.ColPosY, table.Columns.IndexOf(c) + table.PosX].Value = c.Name.Replace("__", "_");
							var i = table.ColPosY;
							switch (c.ColumnType.Name) {
								case "DateTime":
									ws.Cells[table.ColPosY + 1, table.Columns.IndexOf(c) + table.PosX, table.ColPosY + 1 + c.Values.Count, table.Columns.IndexOf(c) + table.PosX].Style.Numberformat.Format = "yyyy MMM dd";
									ws.Column(table.PosX + table.Columns.IndexOf(c)).Width = 15;
									foreach (var v in c.Values) {
										ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = Convert.ToDateTime(v);
									}
									break;
								case "Decimal":
									ws.Cells[table.ColPosY + 1, table.Columns.IndexOf(c) + table.PosX, table.ColPosY + 1 + c.Values.Count, table.Columns.IndexOf(c) + table.PosX].Style.Numberformat.Format = "0.".PadRight(c.Digits + 2, '0');
									ws.Column(table.PosX + table.Columns.IndexOf(c)).Width = Math.Max(10, c.Values.Max(v => v.ToString().Length));
									try {
										foreach (var v in c.Values) {
											ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = (v?.ToString() != "") ? (decimal?)Convert.ToDecimal(v) : null;
										}
									} catch (Exception) {
										foreach (var v in c.Values) {
											ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = v;
										}
									}
									break;
								default:
									ws.Column(table.PosX + table.Columns.IndexOf(c)).Width = Math.Max(10, c.Values.Where(v=>v != null).Max(v => v.ToString().Length + 1));
									foreach (var v in c.Values) {
										ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = v;
									}
									break;
							}
						}
						using (ExcelRange Rng = ws.Cells[table.PosY, table.PosX, table.PosY + table.Columns[0].Values.Count, table.PosX + table.Columns.Count - 1]) {
							ExcelTableCollection tblcollection = ws.Tables;
							ExcelTable ootable = tblcollection.Add(Rng, "tblSalesman");
							ootable.ShowFilter = true;
							ootable.ShowHeader = true;
						}
					}
				}
			}

			byte[] bin = Ep.GetAsByteArray();
			if (!Directory.Exists(Path.GetDirectoryName(FullPath))) {
				var name = Path.GetFileName(FullPath);
				FullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
			}
			File.WriteAllBytes(FullPath, bin);
		}

		public static void WriteToFile(Page page, string Path) {
			WriteToFile(new List<Page>() { page }, Path);
			//ExcelPackage Ep = new ExcelPackage();
			//Ep.Workbook.Worksheets.Add(page.Name);
			//var ws = Ep.Workbook.Worksheets.FirstOrDefault();
			//foreach (var table in page.Tables) {
			//	ws.Cells[table.PosY, table.PosX, table.PosY, table.PosX + table.Columns.Count - 1].Merge = true;
			//	ws.Cells[table.PosY, table.PosX].Value = table.Name;
			//	foreach (var c in table.Columns) {
			//		ws.Cells[table.ColPosY, table.Columns.IndexOf(c) + table.PosX].Value = c.Name;
			//		var i = table.ColPosY;
			//		foreach (var v in c.Values) {
			//			ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = v;
			//		}
			//	}
			//}

			//byte[] bin = Ep.GetAsByteArray();
			//if (!Directory.Exists(Path)) {
			//	var name = Path.Substring(Path.LastIndexOf(@"\") + 1);
			//	name = name.Substring(0, name.LastIndexOf("."));
			//	Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + name + ".xlsx";
			//}
			//File.WriteAllBytes(Path, bin);
		}
		public static void WriteToFile(Table table, string wsName, string Path) {
			WriteToFile(new List<Page>() { new Page() { Name = wsName, Tables = new List<Table>() { table } } }, Path);
			//ExcelPackage Ep = new ExcelPackage();
			//Ep.Workbook.Worksheets.Add(wsName);
			//var ws = Ep.Workbook.Worksheets.FirstOrDefault();
			//if (table.Name != null) {
			//	ws.Cells[table.PosY, table.PosX, table.PosY, table.PosX + table.Columns.Count - 1].Merge = true;
			//	ws.Cells[table.PosY++, table.PosX].Value = table.Name;
			//}
			//foreach (var c in table.Columns) {
			//	ws.Cells[table.ColPosY, table.Columns.IndexOf(c) + table.PosX].Value = c.Name;
			//	var i = table.ColPosY;
			//	switch (c.ColumnType.Name) {
			//		case "DateTime":
			//			ws.Cells[table.ColPosY + 1, table.Columns.IndexOf(c) + table.PosX, table.ColPosY + 1 + c.Values.Count, table.Columns.IndexOf(c) + table.PosX].Style.Numberformat.Format = "yyyy MMM dd";
			//			ws.Column(table.PosX + table.Columns.IndexOf(c)).Width = 15;
			//			break;
			//		case "Decimal":
			//			ws.Cells[table.ColPosY + 1, table.Columns.IndexOf(c) + table.PosX, table.ColPosY + 1 + c.Values.Count, table.Columns.IndexOf(c) + table.PosX].Style.Numberformat.Format = "# ##0,000000000000";
			//			ws.Column(table.PosX + table.Columns.IndexOf(c)).Width = 15;
			//			break;
			//		default:
			//			break;
			//	}
			//	foreach (var v in c.Values) {
			//		ws.Cells[++i, table.Columns.IndexOf(c) + table.PosX].Value = v;
			//	}
			//}

			//byte[] bin = Ep.GetAsByteArray();
			//if (!Directory.Exists(Path.Substring(0, Path.LastIndexOf(@"\") + 1))) {
			//	var name = Path.Substring(Path.LastIndexOf(@"\") + 1);
			//	name = name.Substring(0, name.LastIndexOf("."));
			//	Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + name + ".xlsx";
			//}
			//File.WriteAllBytes(Path, bin);
		}

		public static void WriteToFile(List<Column> columns, string wsName, string Path) {
			WriteToFile(new Table() { Columns = columns }, wsName, Path);
		}
	}
}