using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.Dynamics.BusinessConnectorNet;

using Bcn = Microsoft.Dynamics.BusinessConnectorNet;


namespace WcfSolicitudPrecio
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "Service1" en el código, en svc y en el archivo de configuración.
    // NOTE: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione Service1.svc o Service1.svc.cs en el Explorador de soluciones e inicie la depuración.
    public class ServiceSolicitudPrecio : IServiceSolicitudPrecio
    {

        Bcn.Axapta axp;  
        public string cadenainterface = ConfigurationManager.ConnectionStrings["interface"].ConnectionString;

        public string cadenainterfacedos = ConfigurationManager.ConnectionStrings["interface2"].ConnectionString;
        public bool conectadoax;

        public string conexionaxapta()
        {
            
            try
            {

                

                //     System.Net.NetworkCredential nc = new System.Net.NetworkCredential(userproxy, passwordproxy, dominio);
             //   System.Net.NetworkCredential nc = new System.Net.NetworkCredential("wsconectorax", "wsc0n3ct@x*", "genesiscr.local");
                System.Net.NetworkCredential nc = new System.Net.NetworkCredential("wsconectorax", "wsc0n3ct@x*", "genesiscr.local");

                axp = new Bcn.Axapta();
                //  axp.LogonAs(nc.UserName, "genesiscr.local", nc,  "jsm", "", "JSM@genesiscrax:2711",  ConfigurationManager.AppSettings["AxCfgFile"]);
                axp.LogonAs(nc.UserName, "genesiscr.local", nc, "JSM", "", "JSM@genesiscrax:2712", "C:\\inetpub\\Desarrollo.axc");
             

                conectadoax = true;
            }
            catch (Exception ex)
            {
                conectadoax = false;
                return ex.Message;


            }


            //     conectadoax = true ;
            return "se conecto exitosamente a axapta ";

        }

         
        public DataSet GetPrecios()
        {
                Int64 transaccion;
                Int32 contador = 0;
               
                Axapta Dynax = new Axapta();
                string _account = "";  /* "001640";*/
                string _salestaker = "";
                string _dataarea = "";
                string o = "";
                string itemid;
                string cantidad;
                string detalle = "";
                string precio_solicitado = "" ;
                string artCompetencia = "";
                string marcaCompetencia = "";
                string pricecompetitor = "";
                string competitorName = ""; 
              conexionaxapta();
              if (conectadoax == true)
              {



                  SqlConnection conn = new SqlConnection(cadenainterface);
                  conn.Open();
                  SqlCommand comand = new SqlCommand("select TransaccionNum, CustAccount, SalesTaker , DataAreaId from SPonline_Enc where TransaccionEstatus=0  order by TransaccionNum desc ", conn);


                  SqlDataReader readerencabezado = comand.ExecuteReader();
                  if (readerencabezado.HasRows)
                  {
                      while (readerencabezado.Read())
                      {
                          transaccion = readerencabezado.GetInt64(0);
                          _account = /*"001640";*/      readerencabezado.GetString(1);
                          _salestaker =/* "0079";*/   readerencabezado.GetString(2);
                          _dataarea =/* "dsr";*/  readerencabezado.GetString(3);



                          SqlCommand cmoanddetalle = new SqlCommand("select COUNT(*) from SPonline_Det where TransaccionNum = " + transaccion, conn);

                          contador = Convert.ToInt32(cmoanddetalle.ExecuteScalar());
                          //     contador = Convert.ToInt32(  cmoanddetalle.ExecuteNonQuery() ) ;
                          if (contador > 0) // verifica que el pedido tenga articulos 
                          {

                              SqlCommand cmdlineaspedido = new SqlCommand("select ItemId , Qty , RequestPrice , CompetitorItem ,CompetitorMarca , CompetitorPrice, CompetitorName from SPonline_Det where TransaccionNum = " + transaccion, conn);
                              SqlDataReader readerdetalle = cmdlineaspedido.ExecuteReader();
                              if (readerdetalle.HasRows)
                              {
                                  while (readerdetalle.Read())
                                  {
                                      itemid = readerdetalle.GetString(0);
                                      detalle = detalle + itemid /*"010298"*/ + "|";
                                      cantidad = Convert.ToString(readerdetalle.GetInt32(1));
                                      detalle = detalle + cantidad /* "3"*/ + "|";
                                      precio_solicitado = Convert.ToString( readerdetalle.GetDecimal(2 ) );
                                      detalle = detalle + precio_solicitado + "|";
                                      artCompetencia = readerdetalle.GetString(3);
                                      detalle = detalle + artCompetencia + "|";
                                      marcaCompetencia = readerdetalle.GetString(4);
                                      detalle = detalle + marcaCompetencia + "|";
                                      pricecompetitor = Convert.ToString(  readerdetalle.GetDecimal(5) );
                                      detalle = detalle + pricecompetitor + "|";
                                      competitorName = readerdetalle.GetString(6);
                                      detalle = detalle + competitorName + "|";
                                  }

                                  char[] charsToTrim = { '|' };
                                  detalle = detalle.TrimEnd(charsToTrim);
                              }


                              try
                              {
                               //   detalle = "011127_289";
                                  o = (string)axp.CallStaticClassMethod("JSM_GenerarPedidoVentaOnLine", "SOLICITUD_PRECIO", _account, _salestaker, _dataarea, detalle);


                              //    axp.Logoff(); 
                              }
                              catch (Exception ex)
                              {
                                  ex.ToString();

                              }



                              SqlCommand comandupdstatus = new SqlCommand("update SPonline_Enc set TransaccionEstatus = 1 where TransaccionNum = " + transaccion, conn);
                              comandupdstatus.ExecuteNonQuery();
                              detalle = "";

                              readerdetalle.Close();

                        } // fin del if cuenta registros 

                          _account = "";
                          _salestaker = "";
                          _dataarea = "";




                     } // fin del while del encabezado 




                  }
                  axp.Dispose();
                  axp.Logoff(); // cierra la sesion de ax 
                  readerencabezado.Close();


                  SqlDataAdapter da = new SqlDataAdapter("select TransaccionNum, CustAccount, SalesTaker from SPonline_Enc  where TransaccionEstatus=0", conn);
                  DataSet ds = new DataSet();
                  da.Fill(ds);

                  SqlCommand comandubicacion = new SqlCommand("exec BORRAR_SESSIONE_APP", conn);
                  comandubicacion.ExecuteNonQuery();

                  conn.Dispose();
                  conn.Close();
                  return ds;
              }
              else
              {
                  DataSet ds = new DataSet();


                  return ds;
              }


        }

      
    }
}
