import axios from "axios"
import * as cheerio from "cheerio"
import fs from "fs"
import https from "https"

const axiosConfig = {
    baseURL: "https://blogtruyenmoi.com/",
}

const client = axios.create(axiosConfig)

const GetData = async (page) => {
    const { data } = await client.get("/page-" + page)
    const res = await client.get("/")
    const cookies = res.headers["set-cookie"]

    return data
}

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms))
}

const prefix = "bl"

export const mangaGenericCrawler = async () => {
    var global = 0
    try {
        const set = new Set()
        const arr = []
        for (let i = 601; i <= 1302; i++) {
            global = i
            console.log("Scrapping page" + global)
            const data = await GetData(i)
            const $ = cheerio.load(data)
            await delay(5000)
            const mangaList = $(".list-mainpage.listview .fl-l")
            mangaList.each((index, element) => {
                const title = $(element).find("a").attr("href")
                const parts = title.split("/")

                arr.push({
                    id: prefix + "-" + parts[1],
                    title: parts[2],
                })
            })
            if (i % 10 == 0 || i == 1302) {
                console.log("send data at" + Date.now())
                axios
                    .post("http://localhost:5197/MangaInfoGeneric", arr, {
                        headers: {
                            "Content-Type": "application/json",
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

                arr.splice(0, arr.length)
            }
        }
        let json = JSON.stringify(arr)
    } catch (error) {
        console.error("An error occurred:", error)
        console.log("error at index: " + global)
    }
}
