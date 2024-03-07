---
nav_order: 5
parent: Quickstart
title: Java example
permalink: /quickstart/java
layout: default
---
# <img src="java.png" width="48"> Java

## Upload a document

Create a `FileUploadExample.java` file, with this code:

```java
import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;

public class FileUploadExample {

  public static void main(String[] args) throws IOException {
    String url = "http://127.0.0.1:9001/upload";
    String filename = "README.md";
    String documentId = "doc01";

    // Set up the file and form data
    File file = new File(filename);
    String boundary = "----WebKitFormBoundary" + Long.toHexString(System.currentTimeMillis());
    String CRLF = "\r\n";
    String charset = StandardCharsets.UTF_8.name();

    // Create the HTTP connection
    HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
    connection.setDoOutput(true);
    connection.setRequestMethod("POST");
    connection.setRequestProperty("Content-Type", "multipart/form-data; boundary=" + boundary);

    // Write the request body
    try (OutputStream output = connection.getOutputStream();
        PrintWriter writer = new PrintWriter(new OutputStreamWriter(output, charset), true)) {
      // Add file data
      writer.append("--" + boundary).append(CRLF);
      writer.append("Content-Disposition: form-data; name=\"file1\"; filename=\"" + file.getName() + "\"").append(CRLF);
      writer.append("Content-Type: " + Files.probeContentType(file.toPath())).append(CRLF); // Use probeContentType
      writer.append(CRLF).flush();
      Files.copy(file.toPath(), output);
      output.flush();
      writer.append(CRLF).flush();

      // Add form data
      writer.append("--" + boundary).append(CRLF);
      writer.append("Content-Disposition: form-data; name=\"documentId\"").append(CRLF);
      writer.append(CRLF).append(documentId).append(CRLF).flush();

      // End of request
      writer.append("--" + boundary + "--").append(CRLF).flush();
    }

    // Get the response
    try (InputStream responseStream = connection.getInputStream();
        BufferedReader reader = new BufferedReader(new InputStreamReader(responseStream))) {
      String line;
      StringBuilder response = new StringBuilder();
      while ((line = reader.readLine()) != null) {
        response.append(line);
      }
      System.out.println(response.toString());
    } finally {
      connection.disconnect();
    }
  }
}
```

Build and run the code:

    javac FileUploadExample.java
    java FileUploadExample

and you should see the successful output:

{: .console }
> ```json
> {"index":"","documentId":"doc01","message":"Document upload completed, ingestion pipeline started"}
> ```

## Query

Create a `AskQuestionExample.java` file, with this code:

```java
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;

public class AskQuestionExample {

    public static void main(String[] args) throws IOException {
        String url = "http://127.0.0.1:9001/ask";
        String jsonInputString = "{\"question\":\"Can I use KM from the command line?\"}";

        // Create the HTTP connection
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        connection.setDoOutput(true);
        connection.setRequestMethod("POST");
        connection.setRequestProperty("Content-Type", "application/json");

        // Write the request body
        try (OutputStream os = connection.getOutputStream()) {
            byte[] input = jsonInputString.getBytes("utf-8");
            os.write(input, 0, input.length);
        }

        // Get the response
        try (BufferedReader br = new BufferedReader(new InputStreamReader(connection.getInputStream(), "utf-8"))) {
            StringBuilder response = new StringBuilder();
            String responseLine;
            while ((responseLine = br.readLine()) != null) {
                response.append(responseLine.trim());
            }
            System.out.println(response.toString());
        } finally {
            connection.disconnect();
        }
    }
}
```

Build and run the code:

    javac AskQuestionExample.java
    java AskQuestionExample

and you should see the successful output:

{: .console }
> ```json
> {"question":"Can I use KM from the command line?","noResult":false,"text":"Yes, you can use Kernel Memory (KM) from the command line. There are several scripts provided in the repository that allow you to interact with KM from the command line.\n\nThe `upload-file.sh` script is a client for command line uploads to Kernel Mem...
> ```