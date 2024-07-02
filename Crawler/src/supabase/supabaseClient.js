import { createClient } from "@supabase/supabase-js"
import { config } from "dotenv"
const supabaseUrl = process.env.SUPABASE_URL
const supabaseKey = process.env.SUPABASE_KEY

export const supabaseClient = createClient(
    "https://wsiycjkzqhuvribiejvq.supabase.co",
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndzaXljamt6cWh1dnJpYmllanZxIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MTk1ODg3OTMsImV4cCI6MjAzNTE2NDc5M30.7tNLfn0tEPov9mjRdTeL0sMSdaDI3ZMPsO7BqqVrn1s"
)
console.log("1")
