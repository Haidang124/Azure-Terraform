resource "azurerm_virtual_network" "vnet" {
  name                = "${var.resource_prefix}-vnet"
  address_space       = [var.cidr]
  resource_group_name = var.rg_name
  location            = var.location
}

resource "azurerm_subnet" "subnet" {
  name                 = var.subnet_name
  resource_group_name  = var.rg_name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = var.address_prefixes
}

resource "azurerm_subnet" "subnet" {
  name                 = "${var.subnet_name}-ag"
  resource_group_name  = var.rg_name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = var.address_prefixes
}



resource "azurerm_virtual_network" "jump-vnet" {
  name                = "${var.resource_prefix}-jump-vnet"
  resource_group_name = var.rg_name
  location            = var.location
  address_space       = [var.cidr]
}


# resource "azurerm_virtual_network" "vnet" {
#   name                = "${var.resource_prefix}-vnet"
#   resource_group_name = var.rg_name
#   location            = var.location
#   address_space       = [var.cidr]
# }