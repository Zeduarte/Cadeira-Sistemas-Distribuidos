using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class Client
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Conectando ao servidor...");
        TcpClient client = new TcpClient("127.0.0.1", 13000);
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };


        try
        {
            Console.WriteLine("Conectado ao servidor. Por favor, insira o ID do cliente (CL_0000):");
            string clientId = Console.ReadLine();


            // Envio do ID do cliente para o servidor
            writer.WriteLine("ID " + clientId);

            // Recebe confirmação do servidor
            string serverResponse = reader.ReadLine();
            Console.WriteLine("Resposta do servidor: " + serverResponse);

            string userInput;

            do
            {

                Console.WriteLine("Digite 'complete' para marcar uma tarefa como concluída, 'request' para solicitar nova tarefa, ou 'quit' para sair:");
                userInput = Console.ReadLine();

                switch (userInput.ToLower())
                {
                    case "complete":
                        Console.WriteLine("Informe o ID da tarefa concluída:");
                        string taskId = Console.ReadLine();
                        writer.WriteLine("TASK_COMPLETE " + taskId);
                        break;
                    case "request":
                        writer.WriteLine("REQUEST_TASK");
                        break;
                    case "quit":
                        writer.WriteLine("QUIT");
                        break;
                    default:
                        Console.WriteLine("Comando desconhecido.");
                        break;
                }

                if (userInput.ToLower() != "quit")
                {
                    serverResponse = reader.ReadLine();
                    Console.WriteLine(serverResponse);
                }

            } while (userInput.ToLower() != "quit");
        }
        catch (Exception e)
        {
            Console.WriteLine("Ocorreu um erro: " + e.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }

        Console.WriteLine("Conexão encerrada. Pressione ENTER para sair...");
        Console.Read();
    }
}
