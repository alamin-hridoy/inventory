import json
import requests

from odoo import api, fields, models
from odoo.exceptions import UserError


class InventoryPilotImport(models.Model):
    _name = "inventorypilot.import"
    _description = "InventoryPilot imported aggregate"
    _rec_name = "inventory_title"

    endpoint_url = fields.Char(required=True, default="https://your-app.onrender.com/api/inventory-aggregates")
    api_token = fields.Text(required=True)
    inventory_title = fields.Char(readonly=True)
    inventory_category = fields.Char(readonly=True)
    item_count = fields.Integer(readonly=True)
    generated_at = fields.Datetime(readonly=True)
    field_line_ids = fields.One2many("inventorypilot.import.field", "import_id", string="Fields", readonly=True)
    raw_json = fields.Text(readonly=True)

    def action_import(self):
        for record in self:
            response = requests.get(record.endpoint_url, params={"token": record.api_token}, timeout=20)
            if response.status_code != 200:
                raise UserError("InventoryPilot import failed: %s %s" % (response.status_code, response.text))

            payload = response.json()
            record.field_line_ids.unlink()
            record.write({
                "inventory_title": payload.get("inventoryTitle"),
                "inventory_category": payload.get("category"),
                "item_count": payload.get("itemCount") or 0,
                "generated_at": payload.get("generatedAtUtc"),
                "raw_json": json.dumps(payload, indent=2),
                "field_line_ids": [(0, 0, self._map_field(field)) for field in payload.get("fields", [])],
            })
        return True

    @api.model
    def _map_field(self, field):
        top_values = field.get("topValues") or []
        return {
            "title": field.get("title"),
            "field_type": field.get("type"),
            "average": field.get("average"),
            "minimum": field.get("min"),
            "maximum": field.get("max"),
            "top_values_json": json.dumps(top_values, indent=2),
        }


class InventoryPilotImportField(models.Model):
    _name = "inventorypilot.import.field"
    _description = "InventoryPilot imported field aggregate"
    _order = "id"

    import_id = fields.Many2one("inventorypilot.import", required=True, ondelete="cascade")
    title = fields.Char(readonly=True)
    field_type = fields.Char(readonly=True)
    average = fields.Float(readonly=True)
    minimum = fields.Float(readonly=True)
    maximum = fields.Float(readonly=True)
    top_values_json = fields.Text(readonly=True)
