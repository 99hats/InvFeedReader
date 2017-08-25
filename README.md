# FeedRead

A proof-of-concept feed reader using the Invention framework.

https://gitlab.com/hodgskin-callan/Invention

### To Use

* In the PCL under Resources create a folder named Text.
* Add a plain text file called newsapi.key
* Get a free key from https://newsapi.org/ and paste it in this file
* Run it!

Works for Android and iOS mobile phones. 

Employs very basic caching and priority queues for preloading images and html in background threads.

Caveats:
Although it runs on Windows the web viewer chokes on javascript in the article view.
No attempt has been made to adjust the layout for tablet form factors.
It doesn't read xml feeds but uses json from newsapi.org instead.
No splashscreen.
