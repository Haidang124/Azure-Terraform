terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = "3.96.0"
    }
  }
}

provider "azurerm" {
 subscription_id = "806ab07f-ea59-4983-9f05-82a09f8eb130"
 client_id = "c1653ed7-948e-4af2-a682-715334fe1f09"
 client_secret = "_YT8Q~k.UBOJ3TZEEzxxOcXwT0Iue.shMIeXKcQd"
 tenant_id = "3cd156ff-ca2f-41b1-b42d-033266784aa2"
 features {}
}