
Location="eastus"

ResourceGroup="km$RANDOM"

#------Set the default Resource Group

# if [[ $(az group exists --resource-group "$ResourceGroup" --output tsv) == true ]]; then
#     echo "Resource Group already exists."
# else
#     az group create --name $ResourceGroup --location $Location
# fi

# az configure --defaults group="$ResourceGroup"

#------Deployment


# Resourse Group
# az deployment group create --resource-group $ResourceGroup  --template-file $BicepFile
# define json that I will send as parameter to az deployemnt    
# az deployment group create --resource-group $ResourceGroup  --template-file $BicepFile --parameters '{"location": {"value": "eastus"}}'
# parameters={"namePrevix": {"value": "123"}}
# --parameters '{ \"policyName\": { \"value\": \"policy2\" } }'
# --parameters parameter1=$var1 parameter2=$var2

az deployment sub create -f main.bicep --location=$Location --parameters location=$Location resourceGroupName=$ResourceGroup -c

az deployment sub create -f main.bicep --location=$Location --parameters location=$Location salt=$RANDOM -c

# jq -n -c --arg st "Hello ${name}" \'{"text": $st}\' > $AZ_SCRIPTS_OUTPUT_PATH