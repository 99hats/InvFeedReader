using Inv;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace InvDefault
{
    public static class Shell
    {
        private static Application _application;

        public static void Install(Inv.Application application)
        {
            _application = application;

            application.Title = "My Project";

            var surface = application.Window.NewSurface();
            surface.Background.Colour = Colour.WhiteSmoke;

            var cache = new Dictionary<int, Panel>();
            cache.Clear();

            var flow = surface.NewFlow();
            //flow.Margin.Set(24);
            var section = flow.AddSection();

            var headerLabel = surface.NewLabel();
            headerLabel.Text = "Section 1";
            section.SetHeader(headerLabel);

            IList<dynamic> items = new List<dynamic>();

            surface.Window.RunTask(Thread =>
            {
                // https://mcweekly-app.firebaseio.com/v6/basic.json?orderBy=%22starttime%22&limitToLast=10&endAt=%22ZZZ%22&startAt=%22%22
                var broker = application.Web.NewBroker("https://mcweekly-app.firebaseio.com/v6/");

                var json = broker.GetTextJsonObject<Dictionary<string, dynamic>>(
                    "basic.json?orderBy=%22starttime%22&limitToLast=60&endAt=%22ZZZ%22&startAt=%22%22");
                items = json.Values.ToList();
                items.Reverse();

                Thread.Post(()=> section.SetItemCount(items.Count));
            });

            Debug.WriteLine($"Width: {application.Window.Width}");
            

            section.ItemQuery += i =>
            {
                if (cache.ContainsKey(i)) return cache[i];

                string uri = items[i].preview_url;
                Debug.WriteLine(uri);

                var graphic = new WebGraphic(_application, surface, uri);

                var max = 200;
                var size = Math.Max(i, 5);
                size = Math.Min(size, 50);
                //graphic.Size.SetWidth(300);
                graphic.Size.SetWidth(surface.Window.Width);
                //graphic.Alignment.TopStretch();
                //graphic.Alignment.StretchCenter();

                var lbl = surface.NewLabel();
                lbl.LineWrapping = true;
                lbl.Text = items[i].title;
                lbl.Margin.Set(16);
                lbl.Size.AutoHeight();
                lbl.Size.AutoMaximumWidth();
                var panel = surface.NewTable();
                var gRow = panel.AddRow();
                gRow.Star();

                var tRow = panel.AddRow();
                tRow.Auto();

                var tCol = panel.AddColumn();
                tCol.Star();

                panel.GetCell(0, 0).Content = graphic;
                panel.GetCell(0, 1).Content = lbl;

                //panel.Margin.Set(8, 4, 32, 4);
                //panel.Padding.Set(8);

                panel.AdjustEvent += () => Debug.WriteLine($"Panel: {panel.Surface.Window.Width}");

                panel.Border.Set(0,0,0,2);
                panel.Border.Colour = Colour.DimGray;

                var button = surface.NewButton();
                button.Content = panel;
                button.SingleTapEvent += () => Debug.WriteLine("Button Pressed");

                cache.Add(i, button);

                return button;
            };

            section.SetItemCount(0);

            //var dock = surface.NewHorizontalDock();
            //dock.Size.Auto();
            //dock.AddClient(flow);

            surface.Content = flow;

            application.Window.Transition(surface);
        }
    }

    public class WebGraphic : Inv.Mimic<Graphic>
    {
        public WebGraphic(Application _application, Surface surface, string uri)
        {
            this.Base = surface.NewGraphic();

            this.Base.Image = Inv.Default.Resources.Images.Logo; ; // put loading image here

            if (!String.IsNullOrEmpty(uri))
                surface.Window.RunTask(Thread =>
                {

                    Debug.WriteLine($"Loading: {uri}");
                    //Thread.Sleep(TimeSpan.FromMilliseconds(500));

                    using (var download = _application.Web.GetDownload(new Uri(uri)))
                    using (var memoryStream = new MemoryStream((int)download.Length))
                    {
                        download.Stream.CopyTo(memoryStream);

                        memoryStream.Flush();

                        var image = new Inv.Image(memoryStream.ToArray(), ".png");

                        Thread.Post(() => { this.Base.Image = image; });
                    }
                });
        }

        public Size Size => Base.Size;
        public Alignment Alignment => Base.Alignment;
    }
}
