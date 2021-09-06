using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EscribirBinarioTexto
{
    class Program
    {
        private static Regex regexLabel = new Regex(@"(^\s?;$)|(Ciclo)");
        private static Regex regexReserved = new Regex(@"(^HALT)|(^DEFAI)|(^DEFI)");

        private static Dictionary<string, int> instrucciones = new Dictionary<string, int>()
        {
            { "NOP", 1 },
            { "DEFI", 0 },
            { "DEFD", 0 },
            { "DEFS", 0 },
            { "DEFAI", 0 },
            { "DEFAD", 0 },
            { "DEFAS", 0 },

            { "ADD", 1 },
            { "SUB", 1 },
            { "MULT", 1 },
            { "DIV", 1 },
            { "MOD", 1 },
            { "INC", 3 },
            { "DEC", 3 },

            { "CMPEQ", 1 },
            { "CMPNE", 1 },
            { "CMPLT", 1 },
            { "CMPLE", 1 },
            { "CMPGT", 1 },
            { "CMPGE", 1 },

            { "JMP", 3 },
            { "JMPT", 3 },
            { "JMPF", 3 },
            { "SETIDX", 3 },
            { "SETIDXK", 5 },

            { "PUSHI", 3 },
            { "PUSHD", 3 },
            { "PUSHS", 3 },
            { "PUSHAI", 3 },
            { "PUSHAD", 3 },
            { "PUSHAS", 3 },
            { "PUSHKI", 5 },
            { "PUSHKD", 3 },
            { "PUSHKS", 0 }, //n+2

            { "POPI", 3 },
            { "POPD", 3 },
            { "POPS", 3 },
            { "POPAI", 3 },
            { "POPAD", 3 },
            { "POPAS", 3 },
            { "POPIDX", 1 },

            { "READI", 3 },
            { "READD", 3 },
            { "READS", 3 },
            { "READAI", 3 },
            { "READAD", 3 },
            { "READAS", 3 },

            { "PRTM", 0 }, //n+2
            { "PRTI", 3 },
            { "PRTD", 3 },
            { "PRTS", 3 },
            { "PRTAI", 3 },
            { "PRTAD", 3 },
            { "PRTAS", 3 },

            { "HALT", 1 },
        };

        private static Dictionary<string, int> etiquetas = new Dictionary<string, int>()
        {
            { ";", 0 },
            { ":", 0 }
        };

        private static Dictionary<string, int> variables = new Dictionary<string, int>();

        private static Dictionary<string, int> etiquetas_def = new Dictionary<string, int>();

        private static Dictionary<string, int> etiquetas_refer = new Dictionary<string, int>();

        //@"^\s?(;)$"
        //"([Ciclo:]|[;])"
        static void Main(string[] args)
        {
            // Lee el archivo
            string path = @"C:/Users/93764/Desktop/pruebas bin/prueba texto 2.txt";

            string result = Path.GetFileName(path);
            Console.WriteLine("Nombre del archivo: '{0}'", result);

            Encoding ascii = Encoding.ASCII;

            byte[] readText = File.ReadAllBytes(path);

            string a = ascii.GetString(readText);

            

            //Console.WriteLine("Contenido del archivo:\n" + a);

            //foreach (byte s in readText)
            //{
            //    Console.Write(s + " ");
            //}
            //Console.WriteLine();

            var rom = AssembleSource(a);
        }

        public static byte[] AssembleSource(string source)
        {
            var rom = new List<byte>();

            UInt16 pointer = 0x200;
            var labels = new Dictionary<string, UInt16>();
            var reserved = new Dictionary<string, UInt16>();

            var lineas = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int tam_seg_cod = 0;

            // calcular la direccion de memoria para las etiquetas (por ahora)
            for (var i = 0; i < lineas.Length; i++)
            {
                var numLinea = i + 1;
                var linea = lineas[i];
                //var linea = TrimAndStripComments(rawLinea);
                string nueva_palabra = "";
                Console.WriteLine("ln" + numLinea + ": " + linea);

                for(int j=0; j<linea.Length; j++)//revisa cada caracter en la linea
                {
                    if (etiquetas.ContainsKey(linea[j].ToString()))
                    {
                        //Console.Write("{0}, {1}\n", nueva_palabra + linea[j], etiquetas[linea[j].ToString()]);
                        if(linea[j] == ':' && !etiquetas_def.ContainsKey(nueva_palabra))//si es una etiqueta nueva
                        {
                            etiquetas_def.Add(nueva_palabra, i);
                        }
                        nueva_palabra = "";
                    }

                    if (linea[j] == ' ')//se detiene cuando encuentra un espacio o llego al final de la linea
                    {
                        //j = linea.Length;
                        
                        if (instrucciones.ContainsKey(nueva_palabra))//si la palabra es reservada
                        {
                            //Console.Write(nueva_palabra +", "+ instrucciones[nueva_palabra]+ " \n");
                            tam_seg_cod += instrucciones[nueva_palabra];

                            if (nueva_palabra.Equals("DEFAI"))
                            {
                                var palabras_linea = linea.Split(' ', ',');
                                //foreach (string p in palabras_linea)
                                //{
                                //    Console.WriteLine(p + "\n");
                                //}
                                if(palabras_linea.Length == 3)
                                {
                                    variables.Add(palabras_linea[1], i);//guarda la variable y su dirección (linea de código)
                                }
                                else//si no son tres elementos la instrucción está mal escrita
                                {
                                    Console.WriteLine($"Error: '{linea}' linea: {numLinea}.");
                                }
                            }
                            else if (nueva_palabra.Equals("DEFI"))
                            {
                                var palabras_linea = linea.Split(' ');
                                if (palabras_linea.Length == 2)
                                {
                                    variables.Add(palabras_linea[1], i);//guarda la variable y su dirección (linea de código)
                                }
                                else//si no son tres elementos la instrucción está mal escrita
                                {
                                    Console.WriteLine($"Error: '{linea}' linea: {numLinea}.");
                                }
                            }
                        }
                        else if (etiquetas_def.ContainsKey(nueva_palabra))//si la palabra es una etiqueta ya definida
                        {
                            //Console.Write(nueva_palabra + ", " + etiquetas_def[nueva_palabra] + " \n");
                            etiquetas_refer.Add(nueva_palabra, i);
                            //Console.Write(nueva_palabra + ", " + etiquetas_refer[nueva_palabra] + " \n");
                        }
                        nueva_palabra = "";
                    }
                    else if ((j == linea.Length - 1) && (linea[j] != ' '))//esto es para que pueda revisar hasta que llegue al final
                    {
                        nueva_palabra += linea[j];
                        if (instrucciones.ContainsKey(nueva_palabra))//si la palabra es reservada
                        {
                            //Console.Write(nueva_palabra +", " + instrucciones[nueva_palabra] + " \n");
                            tam_seg_cod += instrucciones[nueva_palabra];
                        }
                        else if (etiquetas_def.ContainsKey(nueva_palabra))//si la palabra es una etiqueta ya definida
                        {
                            //Console.Write(nueva_palabra + ", " + etiquetas_def[nueva_palabra] + " \n");
                            etiquetas_refer.Add(nueva_palabra, i);
                            //Console.Write(nueva_palabra + ", " + etiquetas_refer[nueva_palabra] + " \n");
                        }
                        nueva_palabra = "";
                    }
                    else
                    {
                        nueva_palabra += linea[j];
                    }
                    
                }

                try
                {
                    if (String.IsNullOrEmpty(linea))
                    {
                        continue;
                    }
                    else if (IsLabel(linea))
                    {
                        // analiza la etiqueta y guarda la direccion de memoria actual
                        var coincide = regexLabel.Match(linea);

                        //
                        var label = coincide.Groups[1].Value;

                        //
                        if (labels.ContainsKey(label))
                            throw new Exception($"Etiqueta duplicada '{label}' encontrada en la linea {numLinea}.");

                        labels.Add(label, pointer);
                    }
                    else if (IsReserved(linea))
                    {
                        // analiza la etiqueta y guarda la direccion de memoria actual
                        var coincide = regexReserved.Match(linea);
                        
                        //
                        var reserv = coincide.Groups[1].Value;

                        //
                        if (labels.ContainsKey(reserv))
                            throw new Exception($"Etiqueta duplicada '{reserv}' encontrada en la linea {numLinea}.");

                        labels.Add(reserv, pointer);
                    }
                    else
                    {
                        //en este caso es porque no es una etiqueta
                        throw new Exception($"Error: '{linea}' linea: {numLinea}.");
                    }
                    /*variables
                    palabras reservadas (instrucciones) 
                    constantes (string, int, double)
                    caracteres especiales (, : ;)
                    */
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: '{linea}' linea: {numLinea}.");
                }

            }

            Console.WriteLine("Tamaño segmento de código: "+ tam_seg_cod);

            Console.WriteLine("\nVARIABLES Y DIRECCIÓN (LÍNEA DEL CÓDIGO)");
            foreach (var v in variables)
            {
                Console.WriteLine(v);
            }
            Console.WriteLine("\nETIQUETAS DEFINIDAS Y DIRECCIÓN (LÍNEA DEL CÓDIGO)");
            foreach (var e1 in etiquetas_def)
            {
                Console.WriteLine(e1);
            }
            Console.WriteLine("\nETIQUETAS REFERENCIADAS Y DIRECCIÓN (LÍNEA DEL CÓDIGO)");
            foreach (var e2 in etiquetas_refer)
            {
                Console.WriteLine(e2);
            }


            //se supone que ya que lo lea lo debe guardar en la memoria (operaciones)
            return rom.ToArray();
        }

        private static string TrimAndStripComments(string sourceLine)
        {
            sourceLine = sourceLine.Split(";")[0];
            sourceLine = sourceLine.Trim();
            return sourceLine;
        }

        private static bool IsLabel(string linea)
        {
            return regexLabel.IsMatch(linea);
        }

        private static bool IsReserved(string linea)
        {
            return regexReserved.IsMatch(linea);
        }
    }
}
