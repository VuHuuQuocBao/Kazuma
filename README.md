![Untitled-2024-06-30-2324](https://github.com/VuHuuQuocBao/Spaghetti/assets/96562872/812c036e-3a0a-4997-85eb-f66d3201311f)


- Todo:
  +  Crawl Chapter's images => Yolov5 => Cloud
  +  Worker detects storage's threshold => train
  +  Transfer all Crawler to Background job (Bull)
  +  Write Generic Repo for crawler for future type crawler
  +  Write new Crawler for updates new manga + trigger auto fill detail

- Crawl Chapter's images:
  + each node crawls ? images => Yolov5 => Update Back to Supabase (Synchronously)
  + Need to process 336193 * 20 images in 10 hours => each hour need to process 672386 images
