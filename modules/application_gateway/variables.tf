variable "resource_prefix" {
  type        = string
  description = "システム内で使用される共通名称"
}
variable "location" {
  type        = string
  description = "リソースを配置するロケーション"
}
variable "rg_name" {
  type        = string
  description = "リソースグループの名称"
}

variable "s_pub_id" {
  type        = string
  description = "public subnet"
}


#api_serevr_nic

variable "api_serevr_nic" {
  type        = string
  description = "ロードバランサーに割り付けるサーバーのNIC"
}


variable "vm_api_nic_name" {
  type        = string
  description = "ロードバランサーに割り付けるサーバーのNICの名称"
}

#--- SSL関係

variable "ssl_key_vault_id" {
  type        = string
  description = "SSL証明書のキー領域ID"
}

variable "ssl_identity_id" {
  type        = string
  description = "SSL証明書にアクセスするためのidentity"
}

variable "ssl_certificate_on_key_vault_name" {
  type        = string
  description = "key_vault上のSSL証明書の名称"
}




# variable "cidr" {}
# variable "vm_api_nic" {}
