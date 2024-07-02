import cron from "node-cron"
import { mangaGenericCrawler } from "../CrawlerService/mangaGenericCrawler.js"
import { mangaDetailsCrawler } from "../CrawlerService/mangaDetailsCrawler.js"
import { mangaChapterImagesCrawler } from "../CrawlerService/mangaChapterImagesCrawler.js"

export const mangaGenericCrawlerJob = cron.schedule(
    "*/5 * * * * *",
    () => {
        console.log("Starting mangaGenericCrawlerJob")
        // mangaGenericCrawler()
    },
    { scheduled: false }
)

export const mangaDetailsCrawlerJob = cron.schedule(
    // "1 0 * * *",
    "*/5 * * * * *",
    () => {
        console.log("Starting mangaDetailsCrawlerJob")
        // mangaDetailsCrawler()
    },
    { scheduled: false }
)

export const mangaChapterImagesCrawlerJob = cron.schedule(
    "*/5 * * * * *",
    () => {
        console.log("Starting mangaChapterImagesCrawlerJob")
        // mangaChapterImagesCrawler()
    },
    { scheduled: false }
)
