# Production. Changes here need approval from the platform team: don't edit this file without asking first.

resource "azurerm_service_plan" "tasks" {
  name                = "plan-tasks-prod"
  resource_group_name = "rg-tasks-prod"
  location            = "australiaeast"
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "tasks" {
  name                = "app-tasks-prod"
  resource_group_name = "rg-tasks-prod"
  location            = "australiaeast"
  service_plan_id     = azurerm_service_plan.tasks.id

  site_config {
    application_stack {
      dotnet_version = "10.0"
    }
  }
}
