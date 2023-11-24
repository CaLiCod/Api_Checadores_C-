using Microsoft.AspNetCore.Mvc;
using System.Text;
using zkemkeeper;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace coneccion_checadores_api.Controllers
{
    [ApiController]
    [Route("checadores")]
    public class Checadores : ControllerBase
    {
        [HttpGet]
        [Route("ObtenerRostros")]
        public dynamic ObtenerRostros([FromQuery] string ip, [FromQuery] int puerto)
        {
            // Crear una instancia de la interfaz correspondiente (suele empezar con "IZKEM. o CZKEM")
            CZKEM zkem = new CZKEM();
            // Conectar al dispositivo (La ip la recivimos en la peticion, usamos el puerto 4370 por defecto)
            bool isConnected = zkem.Connect_Net(ip, puerto);
            string response="";//variable de respuest
            List<object> datos = new List<object>();//Arreglo para almacenas todas las respuestas del checador
            //validamos la conección con el checador
            if (isConnected)
            {
                //proceso de consulta de informacion del checador

                //Declaracion de variables
                string sName = string.Empty;
                string sPassword = string.Empty;
                string sdwEnrollNumber = string.Empty;//codigo de usuario
                string sTmpData = string.Empty;// datos del rostro
                int iTmpLength = 0;
                int idwFaceIndex = 12;
                int privilege = 0;
                bool bEnabled = false;
                

                while (zkem.SSR_GetAllUserInfo(1, out sdwEnrollNumber, out sName, out sPassword, out privilege, out bEnabled))
                {

                    zkem.GetUserFaceStr(1, sdwEnrollNumber, idwFaceIndex, ref sTmpData, ref iTmpLength);
                    //Añadimos al arreglo los datos devueltos por el checados en cada vuelta del ciclo
                    datos.Add(new
                    {
                        id_usuario= sdwEnrollNumber,
                        rostro = sTmpData
                    });
                }
                //Convertimos el arreglo en json
                response = JsonSerializer.Serialize(datos);
            }
            //Si la coneccion Falla
            else
            {
                //Mensaje de error al tratar de conectar con el checador
                response = "Error al conectar con el checador";
            }
            
            return response;
        }

        [HttpPost]
        [Route("RegistrarRostro")]
        public async Task<dynamic> RegistrarRostroAsync([FromQuery] string ip, [FromQuery] int puerto)
        {
            // Crear una instancia de la interfaz correspondiente (suele empezar con "IZKEM. o CZKEM")
            CZKEM zkem = new CZKEM();
            // Conectar al dispositivo (La ip la recivimos en la peticion, usamos el puerto 4370 por defecto)
            bool isConnected = zkem.Connect_Net(ip, puerto);
            //Variable para respuesta al cliente
            string response = "";

            if (isConnected)
            {
                //una vez que conecta con el checador
                //creamos una funcion asincrona para leer el Request.Body, y decodificarlo, dicho body es el que recivimos de la peticion http
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    // Leer el cuerpo de la solicitud
                    string cuerpoSolicitud = await reader.ReadToEndAsync();

                    // Deserializar el cuerpo en este caso debe benir en formato JSON
                    List<CuerpoPost> listaDatos = JsonSerializer.Deserialize<List<CuerpoPost>>(cuerpoSolicitud);

                    registrar_rostros(zkem, listaDatos);
                }
            }
            else
            {
                response = "Error al conecta con el checador...";
            }
            return response;
        }

        //Modelo para deserializar el body en se envia en el post
        class CuerpoPost
        {
            public string id_usuario { get; set; }
            public string rostro { get; set; }
        }
        //Metodo para escribir los rostros
        private static void registrar_rostros(CZKEM coneccion_checador, List<CuerpoPost> caras_a_registrar)
        {
            for (int i = 0; i < caras_a_registrar.Count; i++)
            {
                CuerpoPost una_cara = caras_a_registrar.ElementAt(i);
                string sdwEnrollNumber = una_cara.id_usuario;
                string sTmpData = una_cara.rostro;
                int iTmpLength = 0;
                int idwFaceIndex = 12;
                bool registrado_correctamente = coneccion_checador.SetUserFaceStr(1, sdwEnrollNumber, idwFaceIndex, sTmpData, iTmpLength);
            }
        }
    }
}
