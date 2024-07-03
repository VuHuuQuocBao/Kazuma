import { KafkaClient, Producer } from "kafka-node"

// Create a client instance
const client = new KafkaClient({ kafkaHost: "localhost:9092" })

// Create a producer instance
export const producer = new Producer(client)

// Create a payloads array with topic and messages
const message = {
    mangaName: "element.title",
    imageByte: "123",
    fileName: "123",
}
const payloads = [{ topic: "Chapter-Images", messages: JSON.stringify(message) }]

producer.on("ready", function () {
    console.log("Producer is ready")
    producer.send(payloads, function (err, data) {
        if (err) {
            console.log("Error:", err)
        } else {
            console.log(data)
        }
    })
})

producer.on("error", function (err) {
    console.log(err)
})

export const onReady = (producer, e, payloads) => {
    producer.on(e, function () {
        console.log("Producer is ready")
        producer.send(payloads, function (err, data) {
            if (err) {
                console.log("Error:", err)
            } else {
                console.log(data)
            }
        })
    })
}

export const onError = (producer, e) => {
    producer.on("error", function (err) {
        console.log(err)
    })
}
