using Inv;
using System;
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

            var Surface = application.Window.NewSurface();
            Surface.Background.Colour = Colour.WhiteSmoke;

            var cache = new Dictionary<int, Panel>();

            var flow = Surface.NewFlow();
            flow.Margin.Set(24);
            var section = flow.AddSection();

            var headerLabel = Surface.NewLabel();
            headerLabel.Text = "Section 1";
            section.SetHeader(headerLabel);

            // https://mcweekly-app.firebaseio.com/v6/basic.json?orderBy=%22starttime%22&limitToLast=10&endAt=%22ZZZ%22&startAt=%22%22
            var broker = application.Web.NewBroker("https://mcweekly-app.firebaseio.com/v6/");

            var json = broker.GetTextJsonObject<Dictionary<string, dynamic>>(
                "basic.json?orderBy=%22starttime%22&limitToLast=20&endAt=%22ZZZ%22&startAt=%22%22");

            Debug.WriteLine($"Width: {application.Window.Width}");
            var items = json.Values.ToList();

            section.ItemQuery += i =>
            {
                if (cache.ContainsKey(i)) return cache[i];

                string uri = items[i].preview_url;
                Debug.WriteLine(uri);

                var graphic = new WebGraphic(_application, Surface, uri);

                var max = 200;
                var size = Math.Max(i, 5);
                size = Math.Min(size, 50);
                graphic.Size.SetWidth(300);

                var lbl = Surface.NewLabel();
                lbl.LineWrapping = true;
                lbl.Text = items[i].title;
                lbl.Margin.Set(16);
                lbl.Size.AutoHeight();
                lbl.Size.AutoMaximumWidth();
                var panel = Surface.NewTable();
                var gRow = panel.AddRow();
                gRow.Star();

                var tRow = panel.AddRow();
                tRow.Auto();

                var tCol = panel.AddColumn();
                tCol.Star();

                panel.GetCell(0, 0).Content = graphic;
                panel.GetCell(0, 1).Content = lbl;

                panel.Margin.Set(8, 4, 32, 4);
                panel.Padding.Set(8);

                panel.AdjustEvent += () => Debug.WriteLine($"Panel: {panel.Surface.Window.Width}");



                panel.Background.Colour = Colour.Beige;

                cache.Add(i, panel);

                return panel;
            };

            section.SetItemCount(json.Count);
            var dock = Surface.NewHorizontalDock();
            dock.Size.Auto();
            dock.AddClient(flow);

            Surface.Content = dock;

            application.Window.Transition(Surface);
        }
    }
    public class WebGraphic : Inv.Mimic<Graphic>
    {
        public WebGraphic(Application _application, Surface surface, string uri)
        {
            this.Base = surface.NewGraphic();

            this.Base.Image = Inv.Default.Resources.Images.Logo; ; // put loading image here

            surface.Window.RunTask(Thread =>
            {
                using (var download = _application.Web.GetDownload(new Uri(uri)))
                using (var memoryStream = new MemoryStream((int)download.Length))
                {
                    download.Stream.CopyTo(memoryStream);

                    memoryStream.Flush();

                    var image = new Inv.Image(memoryStream.ToArray(), ".png");

                    Thread.Post(() => this.Base.Image = image);
                }
            });
        }

        public Size Size => Base.Size;
    }

    //public class WebGraphic : Inv.Mimic<Graphic>
    //{
    //    private readonly Application _application;
    //    private readonly string _uri;

    //    public WebGraphic(Application application, Surface surface, string uri)
    //    {
    //        this.Base = surface.NewGraphic();
    //        this.Size = this.Base.Size;

    //        _application = application;
    //        _uri = uri;

    //        this.Base.Image = Inv.Default.Resources.Images.Logo; ; // put loading image here

    //        _application.Window.RunTask(GetImage(_uri));
    //    }

    //    public Size Size { get; set; }

    //    public Action<WindowThread> GetImage(string uri)
    //    {
    //        using (var download = _application.Web.GetDownload(new Uri(uri)))
    //        using (var memoryStream = new MemoryStream((int)download.Length))
    //        {
    //            download.Stream.CopyTo(memoryStream);

    //            memoryStream.Flush();

    //            var image = new Inv.Image(memoryStream.ToArray(), ".png");

    //            this.Base.Image = image;
    //        }
    //        return null;
    //    }
    //}
}
