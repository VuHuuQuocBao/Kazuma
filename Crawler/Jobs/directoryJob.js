import { getDirectorySize } from "../SystemService/folderService.js"
import cron from "node-cron"
import { exec } from "child_process"

let isJobRunning = false

export const detectFolderSizeJob = cron.schedule(
    "*/15 * * * * *",
    async () => {
        if (isJobRunning) {
            console.log("Previous job is still running. Skipping this run.")
            return
        }
        isJobRunning = true

        const baseDirectory = "C:\\Users\\PC\\OneDrive\\Desktop\\Project\\Yolov5"
        const directoryData = await getDirectorySize(baseDirectory)
        if (directoryData.size / 1024 / 1024 > 100) {
            const commandPath = baseDirectory + "\\" + "detect.py"
            const modelPath = baseDirectory + "\\" + "weights" + "\\" + "yolov5s_anime.pt"
            const inputPath = baseDirectory + "\\" + "input"
            const outputPath = baseDirectory + "\\" + "output"
            console.log("condition is met, execute python script")
            const yolov5Path = "C:\\Users\\PC\\OneDrive\\Desktop\\Project\\Yolov5"
            exec(`python ${commandPath} --weights ${modelPath} --source ${inputPath} --output ${outputPath}`, (error, stdout, stderr) => {
                if (error) {
                    console.log(`error: ${error.message}`)
                    isJobRunning = false
                    return
                }
                if (stderr) {
                    console.log(`stderr: ${stderr}`)
                    isJobRunning = false
                    return
                }
                console.log(`stdout: ${stdout}`)

                isJobRunning = false
            })
        } else {
            console.log("condition is not met, skip python script execution")
            isJobRunning = false
        }
    },
    { scheduled: false }
)

detectFolderSizeJob.start()
