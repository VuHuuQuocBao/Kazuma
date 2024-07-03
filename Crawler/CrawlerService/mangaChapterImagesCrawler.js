import axios from "axios"
import * as cheerio from "cheerio"
import fs from "fs"
import https from "https"
import { getDataForChapterCrawler } from "../src/supabase/supabaseService.js"
import { supabaseClient } from "../src/supabase/supabaseClient.js"
import axiosRetry from "axios-retry"
import * as lo from "lodash"
//import { producer, onReady, onError } from "../Kafka/KafkaProducer.js"
import { KafkaClient, Producer } from "kafka-node"

const clientKafka = new KafkaClient({ kafkaHost: "localhost:9092" })

// Create a producer instance
const producer = new Producer(clientKafka)

producer.on("ready", function () {
    console.log("Producer is ready")

    mangaChapterImagesCrawler().then(async (results) => {
        for (var x = 0; x < results.length; x++) {
            for (var i = 0; i < results[x].data.length; i++) {
                const message = {
                    mangaName: results[x].title.replace(/\?/g, ""),
                    imageByte: results[x].data[i].buffer.toString("base64"),
                    fileName: results[x].data[i].fileName,
                }
                const payloads = [{ topic: "Chapter-Images", messages: JSON.stringify(message) }]

                const sendPromise = new Promise((resolve, reject) => {
                    producer.send(payloads, function (err, data) {
                        if (err) {
                            console.error("Error:", err)
                            reject(err)
                        } else {
                            console.log(data)
                            resolve(data)
                        }
                    })
                })
                await sendPromise
            }
        }
    })
})

producer.on("error", function (err) {
    console.log(err)
})

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms))
}
const axiosBaseConfig = {
    baseURL: "https://blogtruyenmoi.com/",
}

const axiosChapterConfig = {
    responseType: "stream",
    headers: {
        Accept: "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8",
        "Accept-Encoding": "gzip, deflate, br, zstd",
        "Accept-Language": "en-US,en;q=0.9,vi;q=0.8",
        "Cache-Control": "no-cache",
        Pragma: "no-cache",
        Priority: "i",
        Referer: "https://blogtruyenmoi.com/",
        "Sec-Ch-Ua": '"Not/A)Brand";v="8", "Chromium";v="126", "Microsoft Edge";v="126"',
        "Sec-Ch-Ua-Mobile": "?0",
        "Sec-Ch-Ua-Platform": '"Windows"',
        "Sec-Fetch-Dest": "image",
        "Sec-Fetch-Mode": "no-cors",
        "Sec-Fetch-Site": "cross-site",
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0",
    },
}

const client = axios.create(axiosBaseConfig)

const GetData = async (path) => {
    const { data } = await client.get(path)
    return data
}

export const mangaChapterImagesCrawler = async () => {
    const processData = await getDataForChapterCrawler(supabaseClient, 2)

    const finalResult = []

    for (const element of processData) {
        const parts = element.id?.split("-")
        const slugId = parts[parts.length - 1]
        const slugName = element.title
        const path = `${slugId}/${slugName}`
        var tempResult = await Process(element, path)
        finalResult.push({
            title: element.title,
            data: tempResult,
        })
    }
    return finalResult
}

const Process = async (element, path) => {
    try {
        const data = await GetData(path)

        const $ = cheerio.load(data)

        const listChaptershtml = $(".list-wrap")
        const listChapterHref = listChaptershtml
            .find("a")
            .map((i, el) => {
                return $(el).attr("href")
            })
            .get()

        const listChapterReverse = listChapterHref.reverse()
        for (var i = 0; i < 2; i++) {
            //for (var i = 0; i < listChapterReverse.length; i++) {
            var chapterHtml
            try {
                chapterHtml = await GetData(listChapterReverse[i])
            } catch (error) {
                continue
            }

            const $ = cheerio.load(chapterHtml)

            const listChapterImagesURL = $("#content")
                .find("img")
                .map((i, el) => {
                    return $(el).attr("src")
                })
                .get()
            var results = []
            for (var j = 0; j < listChapterImagesURL.length; j++) {
                let result = await new Promise((resolve, reject) => {
                    const chapterClient = axios.create(axiosChapterConfig)

                    chapterClient
                        .get(listChapterImagesURL[j], {
                            responseType: "stream",
                        })
                        .then((response) => {
                            let buffers = []

                            response.data.on("data", (chunk) => {
                                buffers.push(chunk)
                            })

                            response.data.on("end", () => {
                                let buffer = Buffer.concat(buffers)
                                resolve(buffer)
                            })

                            response.data.on("error", (err) => {
                                reject(err)
                            })
                        })
                })

                results.push({
                    fileName: listChapterImagesURL[j].split("/").pop(),
                    buffer: result,
                })
            }
            return results
        }
    } catch (error) {
        // compensate for any failure, upsert change lock to false
    }
}

//     .then(function (response) {
//         response.data.pipe(fs.createWriteStream("./image2.png"));
//     })
//     .catch((error) => {
//         console.error(error);
//     });

// fs.writeFile("./CrawlData/GenericData/data.json", json, "utf8", function (err) {
//     if (err) throw err;
//     console.log("complete");

//     axios
//         .post("http://localhost:5197/MangaInfoGeneric", arr, {
//             headers: {
//                 "Content-Type": "application/json",
//             },
//             httpsAgent: new https.Agent({
//                 rejectUnauthorized: false,
//             }),
//         })
//         .then((response) => {
//             console.log(response.data);
//         })
//         .catch((error) => {
//             console.error(error);
//         });
// });
