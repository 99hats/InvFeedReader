using Inv;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private static Dictionary<string, bool> _loadingManager = new Dictionary<string, bool>();

        public static void Install(Inv.Application application)
        {
            _application = application;
            _application.Title = "News Feed";

            // main surface
            var mainSurface = application.Window.NewSurface();
            mainSurface.Background.Colour = Colour.LightGray;

            // sub surface
            Surface articleSurface;

            // grap api key
            string apiKey = System.Text.Encoding.UTF8.GetString(Inv.Default.Resources.Text.Newsapi.GetBuffer());

            // main listview
            var flow = mainSurface.NewFlow();
            mainSurface.Content = flow;
            _application.Window.Transition(mainSurface).Fade();

            // section
            var section = flow.AddSection();
            var headerLabel = mainSurface.NewLabel();
            headerLabel.Text = "Tech Crunch";
            headerLabel.JustifyCenter();
            headerLabel.Alignment.TopStretch();
            headerLabel.Padding.Set(0, (mainSurface.Window.Height / 3), 0, 4);
            headerLabel.Background.Colour = Colour.DodgerBlue;
            headerLabel.Font.Colour = Colour.White;
            headerLabel.Font.Size = 32;
            headerLabel.Font.Heavy();
            section.SetHeader(headerLabel);

            IList<Article> items = new List<Article>();


            //var bgQueue = new BackgroundQueue();
            //bgQueue.QueueTask(() => LongRunningTask());

            // fetch feed
            mainSurface.Window.RunTask(thread =>
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

                var cellPanel = new FeedItemPanel(_application, mainSurface, article);
                cache[i] = cellPanel;

                void OnCellPanelOnSingleTapEvent()
                {
                    articleSurface = _application.Window.NewSurface();
                    var browser = articleSurface.NewBrowser();

                    var uriString = @"https://mercury.postlight.com/amp?url=" + article.url;

                    if (htmlCache.ContainsKey(article.url))
                    {
                        var html = htmlCache[article.url];
                        browser.LoadHtml(html);
                        articleSurface.Content = browser;
                    }
                    else
                    {
                        var uri2 = new Uri(uriString);
                        Debug.WriteLine(uri2.AbsoluteUri);
                        browser.LoadUri(uri2);
                        articleSurface.Content = browser;
                    }

                    // preload next webpage
                    if (i < section.ItemCount - 1)
                    {
                        mainSurface.Window.RunTask(Thread =>
                        {
                            var nextItem = items[i + 1];
                            var key = @"https://mercury.postlight.com/amp?url=" + nextItem.url;
                            var uri3 = new Uri(key);
                            var broker = _application.Web.NewBroker($"{uri3.Scheme}://{uri3.DnsSafeHost}");
                            var html = broker.GetPlainText(uri3.PathAndQuery);

                            Thread.Post(() => { htmlCache[nextItem.url] = html; });
                        });
                    }

                    _application.Window.Transition(articleSurface).CarouselNext();
                    articleSurface.GestureBackwardEvent += () =>
                    {
                        _application.Window.Transition(mainSurface).CarouselBack();
                    };
                }

                cellPanel.SingleTapEvent += OnCellPanelOnSingleTapEvent;

                return cellPanel;

            };
        }

        //private static void LongRunningTask()
        //{
        //    Task.Delay(5000);
        //    Debug.WriteLine("LongRunningTask ran");
        //}

        /// <summary>
        /// Returns a MD5 hash as a string
        /// </summary>
        /// <param name="TextToHash">String to be hashed.</param>
        /// <returns>Hash as string.</returns>
        public static String GetMD5Hash(String TextToHash)
        {
            //Check wether data was passed
            if ((TextToHash == null) || (TextToHash.Length == 0))
            {
                return String.Empty;
            }

            //Calculate MD5 hash. This requires that the string is splitted into a byte[].
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
            byte[] result = md5.ComputeHash(textToHash);

            //Convert result back to string.
            return System.BitConverter.ToString(result);
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

            var cacheFileName = Shell.GetMD5Hash(uri) +".jpg";
            var files = _application.Directory.RootFolder.GetFiles("*.jpg");

            Debug.WriteLine($"cacheFileName: {cacheFileName}");
            var cache = files.FirstOrDefault(x => x.Name == cacheFileName);
            if (cache !=null) 
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

    public class BackgroundQueue
    {
        private Task previousTask = Task.FromResult(true);
        private object key = new object();
        public Task QueueTask(Action action)
        {
            lock (key)
            {
                previousTask = previousTask.ContinueWith(t => action()
                    , CancellationToken.None
                    , TaskContinuationOptions.None
                    , TaskScheduler.Default);
                return previousTask;
            }
        }

        public Task<T> QueueTask<T>(Func<T> work)
        {
            lock (key)
            {
                var task = previousTask.ContinueWith(t => work()
                    , CancellationToken.None
                    , TaskContinuationOptions.None
                    , TaskScheduler.Default);
                previousTask = task;
                return task;
            }
        }
    }



    #endregion helpers
}
