using Inv;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InvDefault
{
    public static class Shell
    {
        private static Application _application;

        public static void Install(Inv.Application application)
        {
            _application = application;
            _application.Title = "FeedRead";

            // main surface
            var feedSurface = application.Window.NewSurface();
            feedSurface.Background.Colour = Colour.LightGray;

            // sub surface
            Surface articleSurface;

            // grap api key
            string apiKey = System.Text.Encoding.UTF8.GetString(Inv.Default.Resources.Text.Newsapi.GetBuffer());

           var priorityQueue = new InvDefault.SimplePriorityQueue<string,int>();

            // main listview
            var flow = feedSurface.NewFlow();
            _application.Window.Transition(feedSurface).Fade();
            feedSurface.Content = flow;

            // dumb caching
            var panelCache = new Dictionary<int, Panel>();
            var htmlCache = new Dictionary<string, string>();

            var headerLabel = feedSurface.NewLabel();
            headerLabel.Text = "FeedRead";
            headerLabel.JustifyCenter();
            headerLabel.Alignment.TopStretch();
            headerLabel.Padding.Set(0, 12, 0, 4);
            headerLabel.Background.Colour = Colour.DodgerBlue;
            headerLabel.Font.Colour = Colour.White;
            headerLabel.Font.Size = 32;
            headerLabel.Font.Heavy();

            // section
            var section = flow.AddSection();
            section.SetHeader(headerLabel);

            IList<Article> items = new List<Article>();

            // pull-to-refresh made easy! except on windows
            flow.RefreshEvent += (FlowRefresh obj) =>
            {
                Debug.WriteLine($"Refresh listview");
                feedSurface.Window.RunTask((rThread) =>
                {
                    var feedObject =LoadArticles();
                    rThread.Post(() => {
                        headerLabel.Text = feedObject.source;
                        items = feedObject.articles.ToList();
                        section.SetItemCount(items.Count);
                        foreach (var article in items)
                        {
                            priorityQueue.Enqueue($"cache: {article.url}", 20);
                            priorityQueue.Enqueue($"imageCache: {article.urlToImage}", 10);
                        }
                        obj.Complete();
                    });
                });
            };

            FeedObject LoadArticles()
            {
                    var broker = application.Web.NewBroker("https://newsapi.org/v1/");
                    return broker.GetTextJsonObject<FeedObject>(@"articles?source=ars-technica&sortBy=top&apiKey=" + apiKey);
            }

            if (application.Device.IsWindows)
            {
                // I don't do windows
                // err, windows doesn't do flow.Refresh() 
                var json = LoadArticles();
                items = json.articles.ToList();
                section.SetItemCount(items.Count);
            }
            else
            {
                flow.Refresh();
            }


            string fetchWebPage(string url)
            {
                var ampUri = @"https://mercury.postlight.com/amp?url=" + url;
                var uri3 = new Uri(ampUri);
                //Debug.WriteLine($"uri3: {ampUri}");
                var broker = _application.Web.NewBroker($"{uri3.Scheme}://{uri3.DnsSafeHost}");
                return "[cached]" + broker.GetPlainText(uri3.PathAndQuery);
            }

            void cacheImageToDisk(string url)
            {
                var cacheFileName = Helpers.GetMD5Hash(url) + ".jpg";
                var cacheFile = _application.Directory.RootFolder.NewFile(cacheFileName);
                if (cacheFile.Exists()) return; 
                    using (var download = _application.Web.GetDownload(new Uri(url)))

                    using (var memoryStream = new MemoryStream((int) download.Length))
                    {
                        
                        download.Stream.CopyTo(memoryStream);
                        memoryStream.Flush();
                        cacheFile.WriteAllBytes(memoryStream.ToArray());
                    }
            }


            // Background task queue
            feedSurface.Window.RunTask((WindowThread Thread) =>
            {
                while (true)
                {
                    if (priorityQueue.Count > 0)
                    {
                        string response = priorityQueue.Dequeue();
                        Debug.WriteLine($"{priorityQueue.Count}: {response}");

                        var split = response.Split(new char[] { ':' }, 2);
                        var cmd = split[0].Trim();
                        var cmdParam = split[1].Trim();
                        switch (cmd)
                        {
                            case "cache":
                                //Debug.WriteLine($"Cache: {cmdParam}");
                                if (htmlCache.ContainsKey(cmdParam)) break;
                                var html = fetchWebPage(cmdParam);
                                Thread.Post(()=> htmlCache.Add(cmdParam, html));
                                break;

                            case "imageCache":
                                //Debug.WriteLine($"imageCache: {cmdParam}");
                                cacheImageToDisk(cmdParam);
                                break;

                            case "log":
                                Debug.WriteLine($"Log: {cmdParam}");
                                break;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("sleep...");
                        Thread.Sleep(TimeSpan.FromMilliseconds(5000));
                    }
                }
            });

            feedSurface.LeaveEvent += () =>
            {
                // it's not you, it's me
                Debug.Write("mainSurface.LeaveEvent");
            };


            section.ItemQuery += i =>
            {
                // simple caching
                if (panelCache.ContainsKey(i)) return panelCache[i];

                var article = items[i];

                var cellPanel = new FeedItemPanel(_application, feedSurface, article);

                // cache panel
                panelCache[i] = cellPanel;

                cellPanel.SingleTapEvent += () =>
                {
                    articleSurface = _application.Window.NewSurface();
                    var browser = articleSurface.NewBrowser();

                    // get AMP optimized pages for mobile
                    var ampUri = @"https://mercury.postlight.com/amp?url=" + article.url;

                    // use cache
                    if (htmlCache.ContainsKey(article.url))
                    {
                        var html = htmlCache[article.url];
                        browser.LoadHtml(html);
                        articleSurface.Content = browser;
                    }
                    else
                    {
                        var uri2 = new Uri(ampUri);
                        Debug.WriteLine(uri2.AbsoluteUri);
                        browser.LoadUri(uri2);
                        articleSurface.Content = browser;
                        htmlCache.Add(article.url, browser.Html);
                    }


                    // aah, simple transition
                    _application.Window.Transition(articleSurface).CarouselNext();

                    articleSurface.GestureBackwardEvent += () =>
                    {
                        // and simple return
                        _application.Window.Transition(feedSurface).CarouselBack();
                    };
                };

                return cellPanel;

                

            };
        }


    }

    #region models
    // modeled using json2csharp.net
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

    #region panels
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

        // events are too fancy for a single expression bodies
        public event Action SingleTapEvent
        {
            add => this.Base.SingleTapEvent += value;
            remove => this.Base.SingleTapEvent -= value;
        }
    }

    public class WebGraphic : Inv.Mimic<Graphic>
    {
        public WebGraphic(Application _application, Surface surface, string uri)
        {
            this.Base = surface.NewGraphic();
            this.Base.Image = Inv.Default.Resources.Images.Placeholder; // put loading image here

            var cacheFileName = Helpers.GetMD5Hash(uri) + ".jpg";
            var files = _application.Directory.RootFolder.GetFiles("*.jpg");

            Debug.WriteLine($"cacheFileName: {cacheFileName}");
            var cache = files.FirstOrDefault(x => x.Name == cacheFileName);
            if (cache != null)
            {
                //var cache = _application.Directory.RootFolder.NewFile(cacheFileName);
                var thebytes = cache.ReadAllBytes();
                var imagee2 = new Inv.Image(thebytes, ".jpg");
                this.Base.Image = imagee2;
                return;

            }

            if (!String.IsNullOrEmpty(uri))
                surface.Window.RunTask(Thread =>
                {
                    using (var download = _application.Web.GetDownload(new Uri(uri)))

                    using (var memoryStream = new MemoryStream((int)download.Length))
                    {
                        var cacheFile = _application.Directory.RootFolder.NewFile(cacheFileName);

                        download.Stream.CopyTo(memoryStream);
                        memoryStream.Flush();

                        cacheFile.WriteAllBytes(memoryStream.ToArray());

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

    #endregion helpers
}
