resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_prefix}-rg"
  location = var.location
}

resource "azurerm_resource_group" "backup-rg" {
  name     = "${var.resource_prefix}-backup-rg"
  location = var.location-backup
}
