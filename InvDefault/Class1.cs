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

            _application.Title = "News Feed";


            // main surface
            var surface = application.Window.NewSurface();
            surface.Background.Colour = Colour.LightGray;


            // sub surface
            Surface sf2;

            // grap api key
            string apiKey = System.Text.Encoding.UTF8.GetString(Inv.Default.Resources.Text.Newsapi.GetBuffer());

            // main listview
            var flow = surface.NewFlow();
            surface.Content = flow;
            _application.Window.Transition(surface).Fade();

            // section
            var section = flow.AddSection();
            var headerLabel = surface.NewLabel();
            headerLabel.Text = "Tech Crunch";
            headerLabel.JustifyCenter();
            headerLabel.Alignment.TopStretch();
            headerLabel.Padding.Set(0, (surface.Window.Height / 3), 0, 4);
            headerLabel.Background.Colour = Colour.DodgerBlue;
            headerLabel.Font.Colour = Colour.White;
            headerLabel.Font.Size = 32;
            headerLabel.Font.Heavy();
            section.SetHeader(headerLabel);

            IList<Article> items = new List<Article>();


            // fetch feed
            surface.Window.RunTask(thread =>
            {
                var broker = application.Web.NewBroker("https://newsapi.org/v1/");
                var json = broker.GetTextJsonObject<FeedObject>(@"articles?source=techcrunch&sortBy=latest&apiKey=" +
                                                                apiKey);
                items = json.articles.ToList();
                thread.Post(() => section.SetItemCount(items.Count));
            });

            var cache = new Dictionary<int, Panel>();
            var htmlCache = new Dictionary<string, string>();
            htmlCache.Clear();

            section.ItemQuery += i =>
            {
                // simple caching
                if (cache.ContainsKey(i)) return cache[i];

                var article = items[i];

                var cellPanel = new FeedItemPanel(_application, surface, article);
                cache[i] = cellPanel;

                cellPanel.SingleTapEvent += () =>
                {


                    
                    //application.Web.Launch( new Uri(item.url));



                    sf2 = _application.Window.NewSurface();
                    var browser = sf2.NewBrowser();

                    var uriString = @"https://mercury.postlight.com/amp?url=" + article.url;

                    if (htmlCache.ContainsKey(article.url))
                    {
                        var html = htmlCache[article.url];
                        browser.LoadHtml(html);
                        sf2.Content = browser;
                    }
                    else
                    {

                        var uri2 = new Uri(uriString);
                        Debug.WriteLine(uri2.AbsoluteUri);
                        browser.LoadUri(uri2);

                        sf2.Content = browser;
                    }

                    // preload next webpage
                    if (i < section.ItemCount - 1)
                    {



                        surface.Window.RunTask(Thread =>
                        {
                            var nextItem = items[i + 1];
                            var key = @"https://mercury.postlight.com/amp?url=" + nextItem.url;
                            var uri3 = new Uri(key);
                            var broker = _application.Web.NewBroker($"{uri3.Scheme}://{uri3.DnsSafeHost}");

                            var html = broker.GetPlainText(uri3.PathAndQuery);

                            Thread.Post(() =>
                            {
                                htmlCache[nextItem.url] = html;
                            });
                        });
                    }

                    _application.Window.Transition(sf2).CarouselNext();
                    sf2.GestureBackwardEvent += () => { _application.Window.Transition(surface).CarouselBack(); };
                };

                return cellPanel;

                //string uri = article.urlToImage ?? ""; // in case there's no image




                //var graphic = new WebGraphic(_application, surface, uri);
                //graphic.Size.SetWidth(surface.Window.Width);


                //var lbl = surface.NewLabel();
                //lbl.LineWrapping = true;
                //lbl.Text = article.title;
                //lbl.Margin.Set(16);
                //lbl.Size.AutoHeight();
                //lbl.Size.AutoMaximumWidth();
                //lbl.Font.Size = 18;
                //var panel = surface.NewTable();
                //var gRow = panel.AddRow();
                //gRow.Star();

                //var tRow = panel.AddRow();
                //tRow.Auto();

                //var tCol = panel.AddColumn();
                //tCol.Star();

                //panel.GetCell(0, 0).Content = graphic;
                //panel.GetCell(0, 1).Content = lbl;

                //panel.AdjustEvent += () => Debug.WriteLine($"Panel: {panel.Surface.Window.Width}");

                //Surface sf2;

                //var button = surface.NewButton();
                //button.Background.Colour = Colour.WhiteSmoke;
                //button.Margin.Set(8, 16, 8, 8);
                //button.Content = panel;
                //button.SingleTapEvent += () =>
                //{

                //    //application.Web.Launch( new Uri(item.url));



                //    sf2 = _application.Window.NewSurface();
                //    var browser = sf2.NewBrowser();

                //    var uriString = @"https://mercury.postlight.com/amp?url=" + article.url;

                //    if (htmlCache.ContainsKey(article.url))
                //    {
                //        var html = htmlCache[article.url];
                //        browser.LoadHtml(html);
                //        sf2.Content = browser;
                //    }
                //    else
                //    {

                //        var uri2 = new Uri(uriString);
                //        Debug.WriteLine(uri2.AbsoluteUri);
                //        browser.LoadUri(uri2);

                //        sf2.Content = browser;





                //    }

                //    // preload next webpage
                //    if (i < section.ItemCount - 1)
                //    {



                //        surface.Window.RunTask(Thread =>
                //        {
                //            var nextItem = items[i + 1];
                //            var key = @"https://mercury.postlight.com/amp?url=" + nextItem.url;
                //            var uri3 = new Uri(key);
                //            var broker = _application.Web.NewBroker($"{uri3.Scheme}://{uri3.DnsSafeHost}");

                //            var html = broker.GetPlainText(uri3.PathAndQuery);

                //            Thread.Post(() =>
                //            {
                //                htmlCache[nextItem.url] = html;
                //            });
                //        });
                //    }

                //    _application.Window.Transition(sf2).CarouselNext();
                //    sf2.GestureBackwardEvent += () => { _application.Window.Transition(surface).CarouselBack(); };
                //};


                //cache[i] = button;

                //return button;
            };
        }

    }

    #region models
        public class Article
        {
            public string author { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string urlToImage { get; set; }
            public string publishedAt { get; set; }
        }

        public class FeedObject
        {
            public string status { get; set; }
            public string source { get; set; }
            public string sortBy { get; set; }
            public List<Article> articles { get; set; }
        }
        #endregion

        #region helpers
        public class FeedItemPanel : Inv.Mimic<Button>
        {
            private Application _application;
            private Surface _surface;
            private Article _article;

            public FeedItemPanel(Application application, Surface surface, Article article)
            {
                _application = application;
                _surface = surface;
                _article = article;

                this.Base = surface.NewButton();

                BuildPanel();
            }

            private void BuildPanel()
            {
                var graphic = new WebGraphic(_application, _surface, _article.urlToImage);
                graphic.Size.SetWidth(_surface.Window.Width);

                var lbl = _surface.NewLabel();
                lbl.LineWrapping = true;
                lbl.Text = _article.title;
                lbl.Margin.Set(16);
                lbl.Size.AutoHeight();
                lbl.Size.AutoMaximumWidth();
                lbl.Font.Size = 18;

                var panel = _surface.NewTable();
                var gRow = panel.AddRow();
                gRow.Star();
                var tRow = panel.AddRow();
                tRow.Auto();
                var tCol = panel.AddColumn();
                tCol.Star();
                panel.GetCell(0, 0).Content = graphic;
                panel.GetCell(0, 1).Content = lbl;
                this.Base.Content = panel;
            }

        public event Action SingleTapEvent
        {
            add { this.Base.SingleTapEvent += value; }
            remove { this.Base.SingleTapEvent -= value; }
        }


    }

        public class WebGraphic : Inv.Mimic<Graphic>
        {
            public WebGraphic(Application _application, Surface surface, string uri)
            {
                this.Base = surface.NewGraphic();

                this.Base.Image = Inv.Default.Resources.Images.Placeholder; // put loading image here

                if (!String.IsNullOrEmpty(uri))
                    surface.Window.RunTask(Thread =>
                    {


                        using (var download = _application.Web.GetDownload(new Uri(uri)))

                        using (var memoryStream = new MemoryStream((int) download.Length))
                        {
                            download.Stream.CopyTo(memoryStream);
                            memoryStream.Flush();

                            var image = new Inv.Image(memoryStream.ToArray(), ".jpg");

                            Thread.Post(() =>
                            {
                                var FadeInAnimation = surface.NewAnimation();
                                FadeInAnimation.AddTarget(this.Base).FadeOpacityIn(TimeSpan.FromSeconds(1));
                                this.Base.Image = image;
                                FadeInAnimation.Start();
                            });
                        }
                    });
            }

            public Size Size => Base.Size;
            public Alignment Alignment => Base.Alignment;
        }
        #endregion
}
