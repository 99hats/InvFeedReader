using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Inv;

namespace InvDefault
{
    public static class Shell
    {
        public static void Install(Inv.Application Application)
        {
            Application.Title = "My Project";

            var Surface = Application.Window.NewSurface();
            Surface.Background.Colour = Colour.WhiteSmoke;

            var cache = new Dictionary<int, Panel>();

            var flow = Surface.NewFlow();
            //flow.Size.AutoWidth();
            flow.Margin.Set(24);
            var section = flow.AddSection();



            var headerLabel = Surface.NewLabel();
            headerLabel.Text = "Section 1";
            section.SetHeader(headerLabel);



            Debug.WriteLine($"Width: {Application.Window.Width}");

            section.ItemQuery += i =>
            {

                if (cache.ContainsKey(i)) return cache[i];

                if (section.ItemCount % 60 == 0)
                    section.SetItemCount(section.ItemCount + 60);

                //var graphic = Surface.NewGraphic();
                //graphic.Image = Inv.Default.Resources.Images.Logo;
                var uri = $"https://dummyimage.com/300x{200 + i}/000/fff";
                var graphic = new WebGraphic(Application, Surface, uri);

                var max = 200;
                var size = Math.Max(i, 5);
                size = Math.Min(size, 50);
                graphic.Size.SetHeight(size);


                var lbl = Surface.NewLabel();
                lbl.LineWrapping = true;
                lbl.Text = $" Foo {i}";
                lbl.Margin.Set(16);
                lbl.Size.AutoHeight();
                lbl.Size.AutoMaximumWidth();
                //lbl.Size.SetMaximumWidth(300);
                //lbl.Size.AutoWidth();
                //lbl.Size.AutoMaximumHeight();


                //var panel = Surface.NewTable();


                var panel = Surface.NewTable();
                var row = panel.AddRow();
                row.Auto();

                var gCol = panel.AddColumn();
                gCol.Fixed(124);

                var tCol = panel.AddColumn();
                tCol.Star();

                panel.GetCell(0, 0).Content = graphic;
                panel.GetCell(1, 0).Content = lbl;

                    panel.Size.Auto();
                //panel.Size.AutoWidth();
                panel.Margin.Set(8,4,32,4);
                panel.Padding.Set(8);

                panel.AdjustEvent += () => Debug.WriteLine($"Panel: {panel.Surface.Window.Width}");

                //panel.AddPanel(graphic);

                

                for (int j = 0; j < i; j++)
                {
                    lbl.Text += " bar";
                }
                //panel.AddPanel(lbl);

                panel.Background.Colour = Colour.Beige;

                cache.Add(i, panel);

                return panel;
            };

            section.SetItemCount(60);

            //var section2 = flow.AddSection();

            //var header2Label = Surface.NewLabel();
            //header2Label.Text = "Section 2";
            //section2.SetHeader(header2Label);
            ////section2.ItemQuery += i =>
            //{

            //    if (cache.ContainsKey(i*10000)) return cache[i*10000];

            //    //if (section.ItemCount - i == 20)
            //    //    section.SetItemCount(section.ItemCount + 20);

            //    var graphic = Surface.NewGraphic();
            //    graphic.Image = Inv.Default.Resources.Images.Logo;
            //    var max = 200;
            //    var size = Math.Max(i, 5);
            //    size = Math.Min(size, 50);
            //    graphic.Size.SetHeight(size);

            //    var panel = Surface.NewHorizontalStack();
            //    panel.Size.AutoMaximumHeight();
            //    panel.Size.SetWidth(Application.Window.Width - 16);

            //    panel.AddPanel(graphic);

            //    var lbl = Surface.NewLabel();
            //    lbl.LineWrapping = true;
            //    lbl.Text = $" Foo {i}";
            //    //lbl.Size.SetMaximumWidth(300);
            //    lbl.Size.AutoMaximumHeight();

            //    for (int j = 0; j < i; j++)
            //    {
            //        lbl.Text += " baz";
            //    }
            //    panel.AddPanel(lbl);
            //    panel.Background.Colour = Colour.Beige;

            //    cache.Add(i*10000, panel);

            //    return panel;
            //};
            //section2.SetItemCount(60);



            var dock = Surface.NewHorizontalDock();
            dock.Size.Auto();
            dock.AddClient(flow);


            //dock.AdjustEvent += () =>
            //{
            //    flow.Size.SetWidth(dock.Surface.Window.Width - 124);
            //    Debug.WriteLine($"Surface: {Surface.Content.Surface.Window.Width} Dock: {dock.Surface.Window.Width} Flow: {flow.Surface.Window.Width} ");
            //};

            //flow.AdjustEvent += () => Debug.WriteLine($"Flow: {flow.Surface.Window.Width}");

        Surface.Content = dock;





            //var panel = Surface.NewHorizontalStack();


            //var graphic = Surface.NewGraphic();
            //graphic.Image = Inv.Default.Resources.Images.Logo;
            //graphic.Size.SetMaximumHeight(100);

            //panel.AddPanel(graphic);

            //var Label = Surface.NewLabel();
            //Label.Alignment.Center();
            //Label.Font.Size = 20;
            //Label.Text = $"Invention";
            //panel.AddPanel(Label);

            //panel.Size.SetMaximumHeight(100);

            //Surface.Content = panel;



            Application.Window.Transition(Surface);
        }
    }

    

    public class WebGraphic : Inv.Mimic<Graphic>
    {
        private readonly Application _application;
        private readonly string _uri;

        public WebGraphic(Application application, Surface surface, string uri)
        {
            this.Base = surface.NewGraphic();
            this.Size = this.Base.Size;

            _application = application;
            _uri = uri;

            
            Init();
        }

        public Size Size { get; set; }

        private async void Init()
        {
            this.Base.Image = null; // put loading image here
            this.Base.Image = await GetImage(_uri);
        }

        public async Task<Inv.Image> GetImage(string uri)
        {


            using (var download = _application.Web.GetDownload(new Uri(uri)))
            using (var memoryStream = new MemoryStream((int)download.Length))
            {
                download.Stream.CopyTo(memoryStream);

                memoryStream.Flush();

                var image = new Inv.Image(memoryStream.ToArray(), ".png");

                return image;
            }
        }
    }
}
