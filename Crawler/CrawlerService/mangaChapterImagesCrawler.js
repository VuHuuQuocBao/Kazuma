import axios from "axios"
import * as cheerio from "cheerio"
import fs from "fs"
import https from "https"
import { getDataForChapterCrawler } from "../src/supabase/supabaseService.js"
import { supabaseClient } from "../src/supabase/supabaseClient.js"
import axiosRetry from "axios-retry"
import * as lo from "lodash"

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
    const processData = await getDataForChapterCrawler(supabaseClient, 100)

    for (const element of processData) {
        const parts = element.id?.split("-")
        const slugId = parts[parts.length - 1]
        const slugName = element.title
        const path = `${slugId}/${slugName}`
        await Process(element, path)
    }
}

const Process = async (element, path) => {
    const data = await GetData(path)

    const $ = cheerio.load(data)

    const genresHtmnl = $(".description .category")

    const listChaptershtml = $(".list-wrap")
    const listChapterHref = listChaptershtml
        .find("a")
        .map((i, el) => {
            return $(el).attr("href")
        })
        .get()

    const listChapterReverse = listChapterHref.reverse()
    for (var i = 0; i < listChapterReverse.length; i++) {
        var chapterHtml
        try {
            chapterHtml = await GetData(listChapterReverse[i])
        } catch (error) {
            continue
        }

        const $ = cheerio.load(chapterHtml)

        const listChapterImagesURLTemp = $("#content")

        const listChapterImagesURL = $("#content")
            .find("img")
            .map((i, el) => {
                return $(el).attr("src")
            })
            .get()

        // for (var j = 0; j < listChapterImagesURL.length; j++) {
        //     const chapterClient = axios.create(axiosChapterConfig);
        //     var promise = chapterClient.get(listChapterImagesURL[j], axiosChapterConfig);
        //     promises.push(promise);
        // }
        var promises = []
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

            results.push(result)
            // let promise = new Promise((resolve, reject) => {
            //     const chapterClient = axios.create(axiosChapterConfig);

            //     chapterClient
            //         .get(listChapterImagesURL[j], {
            //             responseType: "stream",
            //         })
            //         .then((response) => {
            //             let buffers = [];

            //             response.data.on("data", (chunk) => {
            //                 buffers.push(chunk);
            //             });

            //             response.data.on("end", () => {
            //                 let buffer = Buffer.concat(buffers);
            //                 resolve(buffer);
            //             });

            //             response.data.on("error", (err) => {
            //                 reject(err);
            //             });
            //         });
            // });

            // promises.push(promise);
        }

        var result = []

        //const values = await Promise.all(promises);

        // for (var k = 0; k < values.length; k++) {
        //     result.push(values[k].data._readableState.buffer);
        // }

        // Promise.all(promises).then((values) => {
        //     const a = 1;
        //     result.push(values);
        // });
        var buffers
        try {
            buffers = await Promise.all(promises)
        } catch (error) {
            console.error(error)
            continue
        }
        var formData = new FormData()

        // for (var k = 0; k < buffers.length; k++) {
        //     let blob = new Blob([buffers[k]], { type: "image/jpeg" });
        //     formData.append("images", blob, `image${k}.jpg`);
        // }

        axiosRetry(axios, {
            retries: 3, // number of retries
            retryDelay: (retryCount) => {
                return retryCount * 1000 // time interval between retries
            },
            retryCondition: (error) => {
                // if retry condition is not specified, by default idempotent requests are retried
                return error.response.status === 500
            },
        })

        for (var k = 0; k < results.length; k++) {
            var formData1 = new FormData()
            let blob = new Blob([results[k]], { type: "image/jpeg" })
            formData1.append("images", blob, `image${k}.jpg`)

            axios
                .post("http://localhost:5197/SaveChapterImages", formData1, {
                    headers: {
                        "Content-Type": "multipart/form-data",
                    },
                    httpsAgent: new https.Agent({
                        rejectUnauthorized: false,
                    }),
                })
                .then((response) => {
                    console.log(response.data)
                })
                .catch((error) => {
                    console.error(error)
                })

            await delay(5000)
        }

        // for (var z = 0; z < formData.length; z++) {
        //     axios
        //         .post("http://localhost:5197/SaveChapterImages", formData[z], {
        //             headers: {
        //                 "Content-Type": "multipart/form-data",
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
        // }

        // await Promise.all(promises).then((buffers) => {
        //     let formData = new FormData();

        //     for (var k = 0; k < buffers.length; k++) {
        //         let blob = new Blob([buffers[k]], { type: "image/jpeg" });
        //         formData.append("images", blob, `image${k}.jpg`);
        //     }

        //     // Send the FormData to the backend
        //     axios
        //         .post("http://localhost:5197/SaveChapterImages", formData, {
        //             headers: {
        //                 "Content-Type": "multipart/form-data",
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
        // let formData = new FormData();
        // for (var k = 0; k < result.length; k++) {
        //     // Convert the buffer to a Blob
        //     let blob = new Blob([new Uint8Array(result[k])], { type: "image/jpeg" });

        //     // Append the Blob to the FormData
        //     formData.append("images", blob, `image${k}.jpg`);
        // }

        // axios
        //     .post("http://localhost:5197/SaveChapterImages", formData, {
        //         headers: {
        //             "Content-Type": "multipart/form-data",
        //         },
        //         httpsAgent: new https.Agent({
        //             rejectUnauthorized: false,
        //         }),
        //     })
        //     .then((response) => {
        //         console.log(response.data);
        //     })
        //     .catch((error) => {
        //         console.error(error);
        //     });

        const a = 1
        //const listChaptersIdString = listChaptersId.reverse().join("|");
    }
}

// axios(config)
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
