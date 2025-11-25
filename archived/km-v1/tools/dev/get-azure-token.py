# Copyright (c) Microsoft. All rights reserved.

# This script generates an Azure access token you might to call
# Azure services directly for some test, e.g. with Postman.

from azure.identity import DefaultAzureCredential

def get_azure_access_token(scope):
    try:
        return DefaultAzureCredential().get_token(scope).token
    except Exception as e:
        print(f"Error generating token: {e}")
        return None

if __name__ == "__main__":
    # Replace the scope with the Azure service you want to call
    scope = "https://cognitiveservices.azure.com/.default"
    access_token = get_azure_access_token(scope)
    if access_token:
        print(f"Access Token: {access_token}")
    else:
        print("Failed to generate access token")
