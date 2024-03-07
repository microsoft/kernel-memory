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
