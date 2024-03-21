locals {

  # http関係
  frontend_http_port_name             = "${var.resource_prefix}-frontend-http-port"
  frontend_http_ip_configuration_name = "${var.resource_prefix}-frontend-http-ip-configuration"

  http_setting_name         = "${var.resource_prefix}-http-setting"
  http_listener_name        = "${var.resource_prefix}-http-listener"
  request_routing_rule_name = "${var.resource_prefix}-request-routing-rule"


  # https関係
  frontend_https_port_name             = "${var.resource_prefix}-frontend-https-port"
  frontend_https_ip_configuration_name = "${var.resource_prefix}-frontend-https-ip-configuration"

  #  https_setting_name   = "${var.resource_prefix}-https-setting"
  https_listener_name  = "${var.resource_prefix}-https-listener"
  ssl_certificate_name = "${var.resource_prefix}-https-certificate"

  request_https_routing_rule_name = "${var.resource_prefix}-https-request-routing-rule"

  backend_address_pool_name = "${var.resource_prefix}-backend-address-pool"

  # probe関係
  probe_name = "main_probe"

}


# Application Gatewayに割り付けするグローバルIPアドレス
resource "azurerm_public_ip" "pip" {
  name                = "${var.resource_prefix}-pip-agw"
  location            = var.location
  resource_group_name = var.rg_name
  allocation_method   = "Static"
  sku                 = "Standard"
}


# key_vault上に存在するSSL証明証
data "azurerm_key_vault_secret" "main" {
  name         = var.ssl_certificate_on_key_vault_name
  key_vault_id = var.ssl_key_vault_id
}


# Application Gatewayの設定
resource "azurerm_application_gateway" "main" {
  name                = "${var.resource_prefix}-agw"
  resource_group_name = var.rg_name
  location            = var.location

  sku {
    name     = "Standard_v2"
    tier     = "Standard_v2"
    capacity = 2
  }

  gateway_ip_configuration {
    name      = "my-gateway-ip-configuration"
    subnet_id = var.s_pub_id
  }

  frontend_port {
    name = local.frontend_http_port_name
    port = 80
  }

  frontend_port {
    name = local.frontend_https_port_name
    port = 443
  }


  frontend_ip_configuration {
    name                 = local.frontend_http_ip_configuration_name
    public_ip_address_id = azurerm_public_ip.pip.id
  }

  backend_address_pool {
    name = local.backend_address_pool_name
  }

  backend_http_settings {
    name                  = local.http_setting_name
    cookie_based_affinity = "Disabled"
    port                  = 5001
    protocol              = "Http"
    request_timeout       = 60
    probe_name            = local.probe_name
  }

  # バックエンドの活性状態監視用プローブ 
  # [ToDo]
  # 活性状態を返すAPIが出来たら置き換える
  probe {
    # hostとはプローブパケットを送信するホストのホスト名
    host                = "127.0.0.1"
    interval            = 30
    minimum_servers     = 0
    name                = local.probe_name
    path                = "/distance?StartPoint.X=0&StartPoint.Y=0&StartPoint.Z=0&EndPoint.X=10&EndPoint.Y=10&EndPoint.Z=5"
    port                = 5001
    protocol            = "Http"
    timeout             = 30
    unhealthy_threshold = 3

    match {
      status_code = []
    }
  }


  # [memo]
  # バックエンドに対しては、HTTPで通信するので、 
  # ここでは、HTTPSの設定は行わない
  #
  # backend_http_settings {
  #   name                                = local.https_setting_name
  #   port                                = 443
  #   protocol                            = "Https"
  #   cookie_based_affinity               = "Disabled"
  #   pick_host_name_from_backend_address = true
  #   request_timeout                     = 20
  # }

  http_listener {
    name                           = local.http_listener_name
    frontend_ip_configuration_name = local.frontend_http_ip_configuration_name
    frontend_port_name             = local.frontend_http_port_name
    protocol                       = "Http"
  }

  http_listener {
    name                           = local.https_listener_name
    frontend_ip_configuration_name = local.frontend_http_ip_configuration_name
    frontend_port_name             = local.frontend_https_port_name
    protocol                       = "Https"
    ssl_certificate_name           = local.ssl_certificate_name
  }

  request_routing_rule {
    name                       = local.request_routing_rule_name
    rule_type                  = "Basic"
    http_listener_name         = local.http_listener_name
    backend_address_pool_name  = local.backend_address_pool_name
    backend_http_settings_name = local.http_setting_name
    priority                   = 10
  }

  request_routing_rule {
    name                       = local.request_https_routing_rule_name
    rule_type                  = "Basic"
    http_listener_name         = local.https_listener_name
    backend_address_pool_name  = local.backend_address_pool_name
    backend_http_settings_name = local.http_setting_name
    priority                   = 1
  }





  # [memo] 
  # key_vaulにアクセスするためにidentityを使って権限を割り付ける
  identity {
    identity_ids = [
      var.ssl_identity_id
    ]
    type = "UserAssigned"
  }

  # SSL証明証
  ssl_certificate {
    name = local.ssl_certificate_name
    data = data.azurerm_key_vault_secret.main.value
  }

}

resource "azurerm_network_interface_application_gateway_backend_address_pool_association" "nic-assoc" {
  #  count                   = 1
  network_interface_id = var.api_serevr_nic
  #  ip_configuration_name   = "nic-ipconfig-${count.index+1}"
  #  ip_configuration_name   = "nic-ipconfig-${count.index+1}"
  ip_configuration_name = var.vm_api_nic_name

  backend_address_pool_id = one(azurerm_application_gateway.main.backend_address_pool).id
}



