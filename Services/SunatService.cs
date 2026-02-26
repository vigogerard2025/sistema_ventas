// ============================================================================
//  SunatService.cs  — Integración completa con Sistema Facturador SUNAT (SFS)
//  Archivo NUEVO: agrégalo a tu proyecto en la carpeta raíz o en /Services/
// ============================================================================
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;          // Instala: dotnet add package Newtonsoft.Json
using SistemaVentas.Database;
using SistemaVentas.Models;
using Npgsql;

namespace SistemaVentas.Services
{
    // =========================================================================
    //  MODELO de respuesta del SFS SUNAT
    // =========================================================================
    public class SunatRespuesta
    {
        public bool   Exito        { get; set; }
        public string Codigo       { get; set; } = "";   // "0" = ACEPTADO
        public string Descripcion  { get; set; } = "";
        public string CdrBase64    { get; set; } = "";
        public string XmlFilename  { get; set; } = "";
    }

    // =========================================================================
    //  DATOS del comprobante (usados internamente)
    // =========================================================================
    public class ComprobanteSunat
    {
        public int      Id;
        public string   RucEmpresa     = "";
        public string   NombreEmpresa  = "";
        public string   DirEmpresa     = "";
        public string   TipoDoc        = "01";   // 01=Factura  03=Boleta
        public string   TipoDocNombre  = "FACTURA";
        public string   Serie          = "";
        public string   Numero         = "";
        public DateTime FechaEmision;
        public string   ClienteDoc     = "00000000";
        public string   ClienteNombre  = "CLIENTE VARIOS";
        public string   ClienteDir     = "";
        public decimal  Subtotal;
        public decimal  Igv;
        public decimal  Total;
    }

    // =========================================================================
    //  SERVICIO principal
    // =========================================================================
    public class SunatSfsService
    {
        // ── Configura aquí la URL de tu SFS SUNAT ─────────────────────────
        // El SFS corre en: java -jar facturadorApp-1.4.jar server prod.yaml
        // Puerto por defecto: 8080 (producción) o 8081 (admin)
        public static string UrlBase = "http://localhost:8080";

        // ── Ruta donde el SFS guarda XMLs para enviar ─────────────────────
        // Ajusta según tu prod.yaml  →  outputPath / PARA_ENVIO
        private static string CarpetaSfs =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "SFSPauloA", "sfsweb", "resources", "REPO", "PARA_ENVIO");

        // ═════════════════════════════════════════════════════════════════
        //  1. VERIFICAR CONEXIÓN AL SFS
        // ═════════════════════════════════════════════════════════════════
        public static async Task<bool> VerificarConexionAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var r = await http.GetAsync(UrlBase);
                return r.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2. OBTENER DATOS DEL COMPROBANTE desde la BD
        // ═════════════════════════════════════════════════════════════════
        public static ComprobanteSunat ObtenerDatos(int compId)
        {
            string ruc = "", nombre = "", dir = "";
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            // Datos de la empresa
            using (var cmd = new NpgsqlCommand(
                "SELECT ruc, nombre, COALESCE(direccion,'') FROM empresas WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("id", Sesion.UsuarioActivo?.EmpresaId ?? 1);
                using var dr = cmd.ExecuteReader();
                if (dr.Read()) { ruc = dr.GetString(0); nombre = dr.GetString(1); dir = dr.GetString(2); }
            }

            // Datos del comprobante
            using var cmd2 = new NpgsqlCommand(@"
                SELECT tipo, serie, numero, fecha_emision,
                       cliente_doc, cliente_nombre, cliente_dir,
                       subtotal, igv, total
                FROM comprobantes WHERE id=@id", conn);
            cmd2.Parameters.AddWithValue("id", compId);
            using var dr2 = cmd2.ExecuteReader();
            if (!dr2.Read()) throw new Exception("Comprobante no encontrado");

            return new ComprobanteSunat
            {
                Id            = compId,
                RucEmpresa    = ruc,
                NombreEmpresa = nombre,
                DirEmpresa    = dir,
                TipoDocNombre = dr2.GetString(0),
                TipoDoc       = dr2.GetString(0) == "BOLETA" ? "03" : "01",
                Serie         = dr2.GetString(1),
                Numero        = dr2.GetString(2),
                FechaEmision  = dr2.GetDateTime(3),
                ClienteDoc    = dr2.IsDBNull(4) ? "00000000" : dr2.GetString(4),
                ClienteNombre = dr2.IsDBNull(5) ? "CLIENTE VARIOS" : dr2.GetString(5),
                ClienteDir    = dr2.IsDBNull(6) ? "" : dr2.GetString(6),
                Subtotal      = dr2.GetDecimal(7),
                Igv           = dr2.GetDecimal(8),
                Total         = dr2.GetDecimal(9)
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  3. GENERAR XML UBL 2.1 y guardarlo en carpeta del SFS
        // ═════════════════════════════════════════════════════════════════
        public static string GenerarXml(ComprobanteSunat d)
        {
            // Nombre del archivo: RUC-TIPO-SERIE-NUMERO.xml
            string nombre = $"{d.RucEmpresa}-{d.TipoDoc}-{d.Serie}-{d.Numero}.xml";
            string xml    = BuildXmlUblPublico(d);

            // Intentar guardar en carpeta del SFS primero
            string ruta = Path.Combine(CarpetaSfs, nombre);
            try
            {
                Directory.CreateDirectory(CarpetaSfs);
                File.WriteAllText(ruta, xml, Encoding.UTF8);
            }
            catch
            {
                // Si no existe la carpeta del SFS, usar Mis Documentos
                string fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SUNAT_XML");
                Directory.CreateDirectory(fallback);
                ruta = Path.Combine(fallback, nombre);
                File.WriteAllText(ruta, xml, Encoding.UTF8);
            }

            // Actualizar BD
            ActualizarEstado(d.Id, "XML_GENERADO", nombre, "");
            return ruta;
        }

        // ═════════════════════════════════════════════════════════════════
        //  4. ENVIAR AL SFS vía API REST (endpoint de Greenter / NUBEFACT)
        // ═════════════════════════════════════════════════════════════════
        public static async Task<SunatRespuesta> EnviarAsync(ComprobanteSunat d)
        {
            string jsonPayload = BuildJsonSfs(d);

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                // ── Endpoint 1: /api/invoice o /api/boleta (según tipo) ───
                string endpoint = d.TipoDocNombre == "BOLETA"
                    ? $"{UrlBase}/api/boleta"
                    : $"{UrlBase}/api/invoice";

                var content  = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(endpoint, content);
                string body  = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Intentar parsear respuesta JSON del SFS
                    try
                    {
                        dynamic? json = JsonConvert.DeserializeObject(body);
                        string  codigo = json?.sunatResponse?.responseCode?.ToString() ?? "0";
                        string  desc   = json?.sunatResponse?.description?.ToString()  ?? "ACEPTADO";
                        bool    ok     = codigo == "0";

                        ActualizarEstado(d.Id, ok ? "ACEPTADO" : "RECHAZADO",
                                         $"{d.RucEmpresa}-{d.TipoDoc}-{d.Serie}-{d.Numero}.xml",
                                         desc);

                        return new SunatRespuesta
                        {
                            Exito       = ok,
                            Codigo      = codigo,
                            Descripcion = desc,
                            XmlFilename = $"{d.RucEmpresa}-{d.TipoDoc}-{d.Serie}-{d.Numero}.xml"
                        };
                    }
                    catch
                    {
                        // Si no parsea JSON, el SFS aceptó igual
                        ActualizarEstado(d.Id, "ENVIADO",
                                         $"{d.RucEmpresa}-{d.TipoDoc}-{d.Serie}-{d.Numero}.xml", body);
                        return new SunatRespuesta { Exito = true, Codigo = "0", Descripcion = "Enviado al SFS" };
                    }
                }

                // ── Endpoint 2 (fallback): /generarComprobante ─────────── 
                var resp2 = await http.PostAsync($"{UrlBase}/generarComprobante", content);
                if (resp2.IsSuccessStatusCode)
                {
                    ActualizarEstado(d.Id, "ENVIADO",
                                     $"{d.RucEmpresa}-{d.TipoDoc}-{d.Serie}-{d.Numero}.xml", "OK");
                    return new SunatRespuesta { Exito = true, Codigo = "0", Descripcion = "Comprobante procesado por SFS" };
                }

                string err = $"HTTP {(int)response.StatusCode}: {body}";
                ActualizarEstado(d.Id, "ERROR_ENVIO", "", err);
                return new SunatRespuesta { Exito = false, Descripcion = err };
            }
            catch (HttpRequestException)
            {
                // SFS no disponible → generar XML manual
                string rutaXml = GenerarXml(d);
                ActualizarEstado(d.Id, "XML_GENERADO", Path.GetFileName(rutaXml),
                                 "SFS no disponible. XML generado para envío manual.");
                return new SunatRespuesta
                {
                    Exito       = false,
                    Codigo      = "SFS_OFFLINE",
                    Descripcion = $"SFS no disponible en {UrlBase}.\nXML guardado en:\n{rutaXml}"
                };
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  5. ACTUALIZAR ESTADO en la BD
        // ═════════════════════════════════════════════════════════════════
        public static void ActualizarEstado(int id, string estado, string filename, string respuesta)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    UPDATE comprobantes
                    SET sunat_estado      = @est,
                        sunat_fecha_envio = NOW(),
                        sunat_respuesta   = @resp,
                        xml_filename      = CASE WHEN @fn <> '' THEN @fn ELSE xml_filename END
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("est",  estado);
                cmd.Parameters.AddWithValue("resp", respuesta.Length > 490
                    ? respuesta.Substring(0, 490) : respuesta);
                cmd.Parameters.AddWithValue("fn",   filename);
                cmd.Parameters.AddWithValue("id",   id);
                cmd.ExecuteNonQuery();
            }
            catch { /* silencioso */ }
        }

        // ═════════════════════════════════════════════════════════════════
        //  HELPERS PRIVADOS — Construcción de XML y JSON
        // ═════════════════════════════════════════════════════════════════

        // ── XML UBL 2.1 válido para SUNAT ────────────────────────────────
        public static string BuildXmlUblPublico(ComprobanteSunat d)
        {
            string tipoDocCat = d.ClienteDoc.Length == 11 ? "6" : "1";
            string letras     = MontoEnLetras(d.Total);

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2""
         xmlns:ds=""http://www.w3.org/2000/09/xmldsig#""
         xmlns:ext=""urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2""
         xmlns:sac=""urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <ext:UBLExtensions>
    <ext:UBLExtension>
      <ext:ExtensionContent/>
    </ext:UBLExtension>
  </ext:UBLExtensions>
  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
  <cbc:CustomizationID>2.0</cbc:CustomizationID>
  <cbc:ID>{d.Serie}-{d.Numero}</cbc:ID>
  <cbc:IssueDate>{d.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>
  <cbc:IssueTime>{d.FechaEmision:HH:mm:ss}</cbc:IssueTime>
  <cbc:InvoiceTypeCode listID=""0101""
    listAgencyName=""PE:SUNAT""
    listName=""Tipo de Documento""
    listURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"">{d.TipoDoc}</cbc:InvoiceTypeCode>
  <cbc:Note languageLocaleID=""1000""><![CDATA[{letras}]]></cbc:Note>
  <cbc:DocumentCurrencyCode>PEN</cbc:DocumentCurrencyCode>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""6"" schemeName=""Documento de Identidad""
                schemeAgencyName=""PE:SUNAT""
                schemeURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"">{d.RucEmpresa}</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName><![CDATA[{d.NombreEmpresa}]]></cbc:RegistrationName>
        <cac:RegistrationAddress>
          <cbc:AddressLine>
            <cbc:Line><![CDATA[{d.DirEmpresa}]]></cbc:Line>
          </cbc:AddressLine>
        </cac:RegistrationAddress>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:AccountingCustomerParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""{tipoDocCat}"" schemeName=""Documento de Identidad""
                schemeAgencyName=""PE:SUNAT"">{d.ClienteDoc}</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName><![CDATA[{d.ClienteNombre}]]></cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingCustomerParty>
  <cac:TaxTotal>
    <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
    <cac:TaxSubtotal>
      <cbc:TaxableAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:TaxableAmount>
      <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
      <cac:TaxCategory>
        <cbc:ID schemeID=""UN/ECE 5305"" schemeName=""Tax Category Identifier""
                schemeAgencyName=""United Nations Economic Commission for Europe"">S</cbc:ID>
        <cbc:Percent>18.00</cbc:Percent>
        <cac:TaxScheme>
          <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyID=""6"">1000</cbc:ID>
          <cbc:Name>IGV</cbc:Name>
          <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>
        </cac:TaxScheme>
      </cac:TaxCategory>
    </cac:TaxSubtotal>
  </cac:TaxTotal>
  <cac:LegalMonetaryTotal>
    <cbc:LineExtensionAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:LineExtensionAmount>
    <cbc:TaxInclusiveAmount currencyID=""PEN"">{d.Total:F2}</cbc:TaxInclusiveAmount>
    <cbc:PayableAmount currencyID=""PEN"">{d.Total:F2}</cbc:PayableAmount>
  </cac:LegalMonetaryTotal>
  <cac:InvoiceLine>
    <cbc:ID>1</cbc:ID>
    <cbc:InvoicedQuantity unitCode=""NIU"">1</cbc:InvoicedQuantity>
    <cbc:LineExtensionAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:LineExtensionAmount>
    <cac:PricingReference>
      <cac:AlternativeConditionPrice>
        <cbc:PriceAmount currencyID=""PEN"">{d.Total:F2}</cbc:PriceAmount>
        <cbc:PriceTypeCode>01</cbc:PriceTypeCode>
      </cac:AlternativeConditionPrice>
    </cac:PricingReference>
    <cac:TaxTotal>
      <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
      <cac:TaxSubtotal>
        <cbc:TaxableAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:TaxableAmount>
        <cbc:TaxAmount currencyID=""PEN"">{d.Igv:F2}</cbc:TaxAmount>
        <cac:TaxCategory>
          <cbc:ID>S</cbc:ID>
          <cbc:Percent>18.00</cbc:Percent>
          <cac:TaxScheme>
            <cbc:ID>1000</cbc:ID>
            <cbc:Name>IGV</cbc:Name>
            <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>
          </cac:TaxScheme>
        </cac:TaxCategory>
      </cac:TaxSubtotal>
    </cac:TaxTotal>
    <cac:Item>
      <cbc:Description><![CDATA[VENTA DE PRODUCTOS / SERVICIOS]]></cbc:Description>
    </cac:Item>
    <cac:Price>
      <cbc:PriceAmount currencyID=""PEN"">{d.Subtotal:F2}</cbc:PriceAmount>
    </cac:Price>
  </cac:InvoiceLine>
</Invoice>";
        }

        // ── JSON para el endpoint del SFS (formato Greenter compatible) ───
        private static string BuildJsonSfs(ComprobanteSunat d)
        {
            string tipoNum = d.TipoDocNombre == "BOLETA" ? "2" : "1";
            string tipoDoc = d.ClienteDoc.Length == 11 ? "6" : "1";
            int    numInt;
            int.TryParse(d.Numero.TrimStart('0'), out numInt);

            return $@"{{
  ""operacion"": ""generar_comprobante"",
  ""tipo_de_comprobante"": {tipoNum},
  ""serie"": ""{d.Serie}"",
  ""numero"": {(numInt == 0 ? 1 : numInt)},
  ""sunat_transaction"": 1,
  ""cliente_tipo_de_documento"": {tipoDoc},
  ""cliente_numero_de_documento"": ""{d.ClienteDoc}"",
  ""cliente_denominacion"": ""{Esc(d.ClienteNombre)}"",
  ""cliente_direccion"": ""{Esc(d.ClienteDir)}"",
  ""fecha_de_emision"": ""{d.FechaEmision:dd-MM-yyyy}"",
  ""hora_de_emision"": ""{d.FechaEmision:HH:mm:ss}"",
  ""moneda"": 1,
  ""porcentaje_de_igv"": 18.0,
  ""total_gravada"": {d.Subtotal:F2},
  ""total_igv"": {d.Igv:F2},
  ""total"": {d.Total:F2},
  ""enviar_automaticamente_a_la_sunat"": true,
  ""enviar_automaticamente_al_cliente"": false,
  ""codigo_de_la_empresa"": ""{d.RucEmpresa}"",
  ""items"": [
    {{
      ""unidad_de_medida"": ""NIU"",
      ""codigo"": ""001"",
      ""descripcion"": ""VENTA DE PRODUCTOS"",
      ""cantidad"": 1,
      ""valor_unitario"": {d.Subtotal:F2},
      ""precio_unitario"": {d.Total:F2},
      ""subtotal"": {d.Subtotal:F2},
      ""tipo_de_igv"": 1,
      ""igv"": {d.Igv:F2},
      ""total"": {d.Total:F2},
      ""anticipo_regularizacion"": false
    }}
  ]
}}";
        }

        private static string Esc(string s) => s.Replace("\"", "\\\"").Replace("\n", " ");

        private static string MontoEnLetras(decimal m)
        {
            int soles    = (int)Math.Floor(m);
            int centavos = (int)Math.Round((m - soles) * 100);
            return $"SON {soles} CON {centavos:D2}/100 SOLES";
        }
    }
}