#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Rect = System.Windows.Rect;

#endregion

// username: jeffs
// created:  2/3/2024 10:39:40 PM

namespace CommonPdfCode.PdfLibrary
{
	public enum AnchorPoint
	{
		BOTTOM = 10,
		CENTER = 20,
		TOP = 30,
		LEFT = 3,
		MIDDLE = 2,
		RIGHT = 1,

		TL = TOP + LEFT,
		TM = TOP + MIDDLE,
		TR = TOP + RIGHT,
		CL = CENTER + LEFT,
		CM = CENTER + MIDDLE,
		CR = CENTER + RIGHT,
		BL = BOTTOM + LEFT,
		BM = BOTTOM + MIDDLE,
		BR = BOTTOM + RIGHT
	}

	public class TextLocation
	{
		public const int IN_TO_POINT_FACTOR = 72;

		// if anchor point is TL, X = distance from page top to text area top
		//		and Y = distance from page left edge to text area left edge
		// if anchor point is BR, X = distance from page bottom to text area bottom
		//		and Y = distance from page right edge to text area right edge

		public string Description { get; }

		public float X { get; }        // anchor point x location (left to right distance)
		public float Y { get; }        // anchor point y location (from top to bottom distance)
		public float W { get; }        // text area width (inches)
		public float H { get; }        // text area height (inches)
		public AnchorPoint AP { get; } // page anchor point

		public TextLocation(float x, float y, float w, float h, AnchorPoint ap, string desc)
		{
			X = x;
			Y = y;
			W = w;
			H = h;
			AP = ap;
			Description = desc;
		}

		/// <summary>
		/// get the rectangle for the text area based on the text location information
		/// adjusting for the anchor point
		/// </summary>
		/// <param name="pw"></param>
		/// <param name="ph"></param>
		/// <param name="pageHeight"></param>
		/// <param name="pageWidth"></param>
		/// <returns></returns>
		public Rectangle GetRect(  float pw, float ph)
		{
			Rectangle rect;

			float bottom = Y;
			float left   = X;


			int anchorVert = ((int) AP) % 10;

			if (AP > AnchorPoint.TOP)
			{
				// is top
				bottom = ph - Y - H;
			}
			else if (AP > AnchorPoint.CENTER)
			{
				// is center
				bottom = ph / 2 - Y - H / 2;
			}


			if (anchorVert == (int) AnchorPoint.RIGHT)
			{
				// is right
				left = pw - X - W;
			}
			else if (anchorVert == (int) AnchorPoint.MIDDLE)
			{
				// is middle
				left = pw / 2 - X - W / 2;
			}


			return new Rectangle(left * IN_TO_POINT_FACTOR, bottom * IN_TO_POINT_FACTOR, W * IN_TO_POINT_FACTOR, H * IN_TO_POINT_FACTOR);
		}

		public void ShowTextLocation(float pw, float ph)
		{
			Rectangle r = GetRect(pw, ph);

			// string b = r.GetBottom().ToString();
			// string l = r.GetLeft().ToString();
			// string w = r.GetWidth().ToString();
			// string h = r.GetHeight().ToString();

			Console.WriteLine("\n");
			Console.WriteLine($"Desc | {Description}");
			Console.WriteLine($"AP | {AP}");
			Console.WriteLine("as inches| page | provided");
			Console.WriteLine($"PW x PH | {pw} x {ph}");
			Console.WriteLine("as inches | text location | provided");
			Console.WriteLine($"X x Y | {X} x {Y}");
			Console.WriteLine($"W x H | {W} x {H}");
			Console.WriteLine("as inches | derived");
			Console.WriteLine($"B x L | {r.GetBottom() / IN_TO_POINT_FACTOR} x {r.GetLeft() / IN_TO_POINT_FACTOR}");
			Console.WriteLine($"w x h | {r.GetWidth() / IN_TO_POINT_FACTOR} x {r.GetHeight() / IN_TO_POINT_FACTOR}");
			Console.WriteLine("as points | derived");
			Console.WriteLine($"B x L | {r.GetBottom()} x {r.GetLeft()}");
			Console.WriteLine($"w x h | {r.GetWidth()} x {r.GetHeight()}");
		}
	}

	public class PdfTextSupport
	{
	#region private fields

		// objects
		private PdfDocument pdfDoc;

		// collections
		private Dictionary<string , int> shtAndPageNumber;

		// misc
		private bool gotShtAndPageNums = false;

	#endregion

	#region ctor

		public PdfTextSupport(PdfDocument pdfDoc)
		{
			this.pdfDoc = pdfDoc;
		}

	#endregion

	#region public properties

	#endregion

	#region private properties

	#endregion

	#region public methods

		public void GetShtAndPageNumbers(TextLocation txLoc)
		{
			for (int i = 0; i < pdfDoc.GetNumberOfPages(); i++)
			{
				string shtNum = GetTextInRect(i, txLoc);
				if (shtNum == null) continue;
				
				shtAndPageNumber.Add(shtNum, i);
				
			}
		}

		public string GetTextInRect(int pageNum, TextLocation txLoc)
		{
			Rectangle pageSize = pdfDoc.GetPage(pageNum).GetPageSize();
			Rectangle rect = txLoc.GetRect(pageSize.GetHeight(), pageSize.GetWidth());

			TextRegionEventFilter regionFilter = new TextRegionEventFilter(rect);

			ITextExtractionStrategy strategy = 
				new FilteredTextEventListener(new LocationTextExtractionStrategy(), regionFilter);

			return PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(pageNum), strategy);
		}

		public List<TextBit> GetTextOnPage(int pageNum)
		{
			PdfPage page = pdfDoc.GetPage(pageNum);

			return PdfTextAndLocationStrategy.GetTextBits(page);

		}

	#endregion

	#region private methods

	#endregion

	#region event consuming

	#endregion

	#region event publishing

	#endregion

	#region system overrides

		public override string ToString()
		{
			return $"this is {nameof(PdfTextSupport)}";
		}

	#endregion

	}

	class PdfTextAndLocationStrategy : LocationTextExtractionStrategy
	{
		public List<TextBit> ResultCoordinates { get; set; }


		public PdfTextAndLocationStrategy()
		{
			ResultCoordinates = new List<TextBit>();
		}

		public static List<TextBit> GetTextBits(PdfPage page)
		{
			PdfTextAndLocationStrategy strategy = new PdfTextAndLocationStrategy();
			PdfTextExtractor.GetTextFromPage(page, strategy);

			return strategy.ResultCoordinates;
		}

		public override void EventOccurred(IEventData data, EventType type)
		{
			if (!type.Equals(EventType.RENDER_TEXT)) return;

			string text;

			TextRenderInfo ri = (TextRenderInfo) data ;

			IList<TextRenderInfo> ris = ri.GetCharacterRenderInfos();

			// string text = ri.GetText();
			// CharacterRenderInfo b = new CharacterRenderInfo(ri);
			// ResultCoordinates.Add(new TextBit(text, b.GetLocation()));

			// Rectangle rb = b.GetBoundingBox();

			// Rectangle rFinal = new Rectangle(0, 0, 0, 0);
			
			for (var i = 0; i < ris.Count; i++)
			{
				text = ris[i].GetText();
				CharacterRenderInfo a = new CharacterRenderInfo(ris[i]);

				ITextChunkLocation r = a.GetLocation();

				ResultCoordinates.Add(new TextBit(text, r));

				// rFinal = addHorizontal(rFinal, r);
			}
			
			// CharacterRenderInfo c = new CharacterRenderInfo(ris[0]);
			
			

		}

		private Rectangle addHorizontal(Rectangle rOrig, Rectangle rAdd)
		{
			float x;
			float y;
			float h;
			float w = 0.0f;

			if (rOrig.GetHeight() == 0.0f)
			{
				x = rAdd.GetX();
				y = rAdd.GetY();
				h = rAdd.GetHeight();
			}

			x = rOrig.GetX() < rAdd.GetX() ? rOrig.GetX() : rAdd.GetX();
			y = rOrig.GetY() > rAdd.GetY() ? rOrig.GetY() : rAdd.GetY();
			h = rOrig.GetHeight() > rAdd.GetHeight() ? rOrig.GetHeight() : rAdd.GetHeight();
			w = rOrig.GetWidth() + rAdd.GetWidth();

			return new Rectangle(x, y, w, h);

		}

	}

	public class TextBit 
	{
		public string Text { get; set; }
		// public Rectangle ResultCoordinates { get; set; }
		public ITextChunkLocation Location { get; set; }
		public TextBit(string s, ITextChunkLocation l) 
		{
			Text = s;
			// ResultCoordinates = r;
			Location = l;
		}
	}

	
	/*
	class RectangleTextExtractionStrategy : ITextExtractionStrategy
	{
		/*
		LocationTextExtractionStrategy strategy =
			new LocationTextExtractionStrategy();

		strategy.SetUseActualText(true);

		RectangleTextExtractionStrategy rs = new RectangleTextExtractionStrategy(strategy, r);
		page = pdfDoc.GetPage(i);
		string text = PdfTextExtractor.GetTextFromPage(page, rs);
		#1#


		private ITextExtractionStrategy innerStrategy = null;
		private Rectangle rectangle;

		public RectangleTextExtractionStrategy(ITextExtractionStrategy strategy, Rectangle rectangle)
		{
			this.innerStrategy = strategy;
			this.rectangle = rectangle;
		}


		public String GetResultantText()
		{
			return innerStrategy.GetResultantText();
		}


		public void EventOccurred(IEventData iEventData, EventType eventType)
		{

			
			if (eventType != EventType.RENDER_TEXT)
				return;
			TextRenderInfo tri = (TextRenderInfo) iEventData;
			foreach (TextRenderInfo subTri in tri.GetCharacterRenderInfos())
			{
				// string t = subTri.GetActualText();
				// string tx = subTri.GetText();
				//
				// IList<TextRenderInfo> b = subTri.GetCharacterRenderInfos();
				//
				// ITextChunkLocation l = a.GetLocation();
				// string ty = a.GetText();

				CharacterRenderInfo a = new CharacterRenderInfo(subTri);

				Rectangle r2 = a.GetBoundingBox();
				if (intersects(r2))
					innerStrategy.EventOccurred(subTri, EventType.RENDER_TEXT);
			}
		}

		private bool intersects(Rectangle rectangle)
		{
			return this.rectangle.Contains(rectangle);
		}

		public ICollection<EventType> GetSupportedEvents()
		{
			return innerStrategy.GetSupportedEvents();
		}
	}
	*/

}