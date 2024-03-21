module "rg" {
  source          = "../../modules/resource-group"
  location        = var.location
  location-backup = "East US"
  resource_prefix = "terraform-nikon"
}

module "vnet" {
  source           = "../../modules/vnet"
  rg_name          = module.rg.name
  cidr             = ""
  subnet_name      = ""
  address_prefixes = ""
  location         = ""
  resource_prefix  = ""
}
