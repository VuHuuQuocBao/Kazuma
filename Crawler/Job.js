import { mangaDetailsCrawlerJob, mangaGenericCrawlerJob, mangaChapterImagesCrawlerJob } from "./Jobs/metadataJob.js"

mangaGenericCrawlerJob.start()
mangaDetailsCrawlerJob.start()
mangaChapterImagesCrawlerJob.start()
