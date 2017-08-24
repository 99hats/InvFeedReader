using Inv;
using Priority_Queue;
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



            //////////////////////////// TEST QUEUE


            SimplePriorityQueue<string> priorityQueue = new SimplePriorityQueue<string>();
            SimplePriorityQueue<Action> actionQueue = new SimplePriorityQueue<Action>();

            //Now, let's add them all to the queue (in some arbitrary order)!
            priorityQueue.Enqueue("4 - Joseph", 4);
            priorityQueue.Enqueue("2 - Tyler", 0); //Note: Priority = 0 right now!
            priorityQueue.Enqueue("1 - Jason", 1);
            priorityQueue.Enqueue("4 - Ryan", 4);
            priorityQueue.Enqueue("3 - Valerie", 3);

            //Change one of the string's priority to 2.  Since this string is already in the priority queue, we call UpdatePriority() to do this
            priorityQueue.UpdatePriority("2 - Tyler", 2);

            //Finally, we'll dequeue all the strings and print them out
            while (priorityQueue.Count != 0)
            {
                string nextUser = priorityQueue.Dequeue();
                Debug.WriteLine(nextUser);
            }


            /////////////////////////////// END TEST QUEUE





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
            Debug.WriteLine($"mainSurface.Window.Height: {_application.Window.Height}");
            headerLabel.Padding.Set(0, (_application.Window.Height / 3), 0, 4);
            headerLabel.Background.Colour = Colour.DodgerBlue;
            headerLabel.Font.Colour = Colour.White;
            headerLabel.Font.Size = 32;
            headerLabel.Font.Heavy();
            headerLabel.AdjustEvent += () =>
            {
                Debug.WriteLine($"mainSurface: {mainSurface.Window.Height}");
                if (mainSurface.Window.Height > 0)
                {
                    headerLabel.Padding.Set(0, (mainSurface.Window.Height / 3), 0, 4);
                }
            };
            section.SetHeader(headerLabel);

            IList<Article> items = new List<Article>();


            //var bgQueue = new BackgroundQueue();
            //bgQueue.QueueTask(() => LongRunningTask());


            #region actionqueue
            //this works!
            // fetch feed
            void loadFeed(WindowThread thread)
            {
                Debug.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>> LOAD FEED");
                var broker = application.Web.NewBroker("https://newsapi.org/v1/");
                var json = broker.GetTextJsonObject<FeedObject>(@"articles?source=techcrunch&sortBy=latest&apiKey=" + apiKey);
                items = json.articles.ToList();
                thread.Post(() => section.SetItemCount(items.Count));
            }

            //mainSurface.Window.RunTask((Thread) => loadFeed(Thread));


            ActionQueue queue = new ActionQueue();

            mainSurface.Window.RunTask((Thread) =>
            {
                queue.Enqueue(() =>
                    loadFeed(Thread));

                queue.Process();

            });

            

            #endregion




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

            var cacheFileName = Shell.GetMD5Hash(uri) + ".jpg";
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



    // https://codereview.stackexchange.com/questions/6826/action-queue-in-net-3-5
    public class ActionQueue
    {
        private Thread _thread;
        private bool _isProcessed = false;
        private object _queueSync = new object();
        private readonly Queue<Action> _actions = new Queue<Action>();
        private SynchronizationContext _context;

        /// <summary>
        /// Occurs when one of executed action throws unhandled exception.
        /// </summary>
        public event CrossThreadExceptionEventHandler ExceptionOccured;

        /// <summary>
        /// Occurs when all actions in queue are finished.
        /// </summary>
        public event EventHandler ProcessingFinished;

        /// <summary>
        /// Gets enqueued actions.
        /// </summary>
        public IEnumerable<Action> Actions
        {
            get
            {
                lock (_queueSync)
                {
                    return new ReadOnlyCollection<Action>(_actions.ToList());
                }
            }
        }

        protected virtual void Execute()
        {
            _isProcessed = true;

            try
            {
                while (true)
                {
                    Action action = null;

                    lock (_queueSync)
                    {
                        if (_actions.Count == 0)
                            break;
                        else
                            action = _actions.Dequeue();
                    }

                    action.Invoke();
                }

                if (ProcessingFinished != null)
                {
                    _context.Send(s => ProcessingFinished(this, EventArgs.Empty), null);
                }
            }
            catch (ThreadAbortException)
            {
                // Execution aborted
            }
            catch (Exception ex)
            {
                if (ExceptionOccured != null)
                {
                    _context.Send(s => ExceptionOccured(this, new CrossThreadExceptionEventArgs(ex)), null);
                }
            }
            finally
            {
                _isProcessed = false;
            }
        }

        /// <summary>
        /// Starts processing current queue.
        /// </summary>
        /// <returns>Returns true if execution was started.</returns>
        public virtual bool Process()
        {
            if (!_isProcessed)
            {
                _context = SynchronizationContext.Current;

                _thread = new Thread(Execute);
                _thread.Start();

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Enqueues action to process.
        /// </summary>
        /// <param name="action">Action to enqueue.</param>
        public void Enqueue(Action action)
        {
            lock (_queueSync)
            {
                Debug.WriteLine("Enqueue");
                _actions.Enqueue(action);
            }
        }

        /// <summary>
        /// Clears queue.
        /// </summary>
        public void Clear()
        {
            lock (_queueSync)
            {
                _actions.Clear();
            }
        }

        /// <summary>
        /// Aborts execution of current queue.
        /// </summary>
        /// <returns>Returns true if execution was aborted.</returns>
        public bool Abort()
        {
            if (_isProcessed)
            {
                _thread.Abort();
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public delegate void CrossThreadExceptionEventHandler(object sender, CrossThreadExceptionEventArgs e);

    public class CrossThreadExceptionEventArgs : EventArgs
    {
        public CrossThreadExceptionEventArgs(Exception exception)
        {
            this.Exception = exception;
        }

        public Exception Exception
        {
            get;
            set;
        }
    }


    #endregion helpers
}
