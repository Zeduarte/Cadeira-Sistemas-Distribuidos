using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using CsvHelper;
using System.Globalization;

public class Server
{


    // Dicionários para cada serviço
    private static Dictionary<string, Dictionary<int, (string Descricao, string Estado, string ClienteId)>> allServices =
        new Dictionary<string, Dictionary<int, (string, string, string)>>()
        {
            {"Serviço_A", new Dictionary<int, (string, string, string)>()
                {
                    { 1, ("Inspeção Rápida de Áreas Verdes", "Disponível", "") },
                    { 2, ("Distribuição de Materiais de Jardinagem", "Disponível", "") },
                    { 3, ("Monitorização de Sistemas de Irrigação", "Disponível", "") },
                    { 4, ("Resposta Rápida a Alertas Ambientais", "Disponível", "") },
                    { 5, ("Comunicação Eficiente", "Disponível", "") },
                    { 6, ("Transporte de Plantas Pequenas", "Disponível", "") }
                }
            },
            {"Serviço_B", new Dictionary<int, (string, string, string)>()
                {
                    { 1, ("Vigilância de Incêndios", "Disponível", "") },
                    { 2, ("Primeira Resposta a Emergências", "Disponível", "") },
                    { 3, ("Inspeção de Hidrantes", "Disponível", "") },
                    { 4, ("Distribuição de Material de Prevenção", "Disponível", "") },
                    { 5, ("Comunicação e Coordenação Rápida", "Disponível", "") },
                    { 6, ("Transporte Rápido de Equipamentos Leves", "Disponível", "") }
                }
            },
            {"Serviço_C", new Dictionary<int, (string, string, string)>()
                {
                    { 1, ("Entrega Rápida de Correio e Pacotes Leves", "Disponível", "") },
                    { 2, ("Coleta de Correspondência", "Disponível", "") },
                    { 3, ("Entrega de Encomendas Urgentes", "Disponível", "") },
                    { 4, ("Manutenção Regular de Mota", "Disponível", "") },
                    { 5, ("Navigação Eficaz", "Disponível", "") },
                    { 6, ("Comunicação Constante", "Disponível", "") }
                }
            },
            {"Serviço_D", new Dictionary<int, (string, string, string)>()
                {
                    { 1, ("Entrega Rápida de Pizzas", "Disponível", "") },
                    { 2, ("Gerenciamento de Pedidos em Trânsito", "Disponível", "") },
                    { 3, ("Promoções Móveis", "Disponível", "") },
                    { 4, ("Inspeção de Segurança para Entregadores", "Disponível", "") },
                    { 5, ("Treinamento em Direção Defensiva", "Disponível", "") },
                    { 6, ("Comunicação Eficiente Durante Entregas", "Disponível", "") }
                }
            }
        };

    public class TaskAssignment
    {
        public int TarefaID { get; set; }
        public string Descricao { get; set; }
        public string Estado { get; set; }
        public string ClienteID { get; set; }
        public string ServicoNome { get; set; }
    }

    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 13000);
        server.Start();
        Console.WriteLine("Servidor iniciado...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
            clientThread.Start(client);
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        string clientId = null;
        Random random = new Random();

        try
        {
            string message;
            while ((message = reader.ReadLine()) != null && message != "QUIT")
            {
                Console.WriteLine("Recebido: " + message);
                string[] parts = message.Split(' ');
                if (parts[0] == "ID")
                {
                    clientId = parts[1];
                    writer.WriteLine("ID recebido");
                }
                else
                {
                    switch (parts[0])
                    {
                       
                        case "TASK_COMPLETE":
                            //writer.WriteLine($"200 Tarefa {parts[1]} completada pelo cliente.");
                            int taskId = int.Parse(parts[1]);
                            string taskResponse = CompleteTask(clientId, taskId);
                            writer.WriteLine(taskResponse);
                            break;

                        case "REQUEST_TASK":
                            if (HasOngoingTask(clientId))
                            {
                                writer.WriteLine("Conclua primeiro a tarefa em curso");
                            }
                            else
                            {
                            var serviceKeys = new List<string>(allServices.Keys);
                            string randomService = serviceKeys[random.Next(serviceKeys.Count)];
                            string taskAssigned = AssignTask(randomService, clientId);
                            writer.WriteLine(taskAssigned);
                            }
                            break;
                        case "QUIT":
                            writer.WriteLine("400 BYE");
                            break;
                        default:
                            writer.WriteLine("Comando desconhecido");
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro: {e.Message}");
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }

    private static bool HasOngoingTask(string clientId)
    {
        foreach (var service in allServices)
        {
            foreach (var task in service.Value)
            {
                if (task.Value.ClienteId == clientId && task.Value.Estado == "Em curso")
                {
                    return true;
                }
            }
        }
        return false;
    }


    private static string AssignTask(string serviceName, string clientId)
    {
        var serviceTasks = allServices[serviceName];
        foreach (var tarefa in serviceTasks)
        {
            if (tarefa.Value.Estado == "Disponível")
            {
                serviceTasks[tarefa.Key] = (tarefa.Value.Descricao, "Em curso", clientId);
                SaveTaskToCsv(serviceName, tarefa.Key, tarefa.Value.Descricao, "Em curso", clientId);
                SaveGeneralAssignmentToCsv(serviceName, tarefa.Key, clientId);
                return $"Id da Tarefa: {tarefa.Key} Serviço: {serviceName}";
            }
        }
        return "404 Nenhuma tarefa disponível";
    }

    private static void SaveTaskToCsv(string serviceName, int tarefaId, string descricao, string estado, string clientId)
    {
        // Cria diretório para o serviço, se necessário
        string directoryPath = Path.Combine(Environment.CurrentDirectory, serviceName);
        Directory.CreateDirectory(directoryPath);  // CreateDirectory verifica se já existe

        // Define o caminho do arquivo dentro do diretório do serviço
        var filePath = Path.Combine(directoryPath, $"{serviceName}.csv");
        var taskAssignment = new TaskAssignment
        {
            TarefaID = tarefaId,
            Descricao = descricao,
            Estado = estado,
            ClienteID = clientId,
            //ServicoNome = serviceName
        };

        bool fileExists = File.Exists(filePath);
        using (var stream = File.Open(filePath, FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            if (!fileExists)
            {
                csv.WriteHeader<TaskAssignment>();
                csv.NextRecord();
            }

            csv.WriteRecord(taskAssignment);
            csv.NextRecord();
        }
    }
    private static string CompleteTask(string clientId, int taskId)
    {
        foreach (var service in allServices)
        {
            if (service.Value.ContainsKey(taskId))
            {
                var task = service.Value[taskId];
                if (task.ClienteId == clientId && task.Estado == "Em curso")
                {
                    service.Value[taskId] = (task.Descricao, "Concluído", clientId);
                    SaveTaskToCsv(service.Key, taskId, task.Descricao, "Concluído", clientId);
                    return $"200 Tarefa {taskId} completada.";
                }
            }
        }
        return "404 Tarefa não encontrada ou estado inválido.";
    }

    public class TaskAssignments
    {
        public string ClienteID { get; set; }
        public int TarefaID { get; set; }
        public string ServicoNome { get; set; }
    }

    private static void SaveGeneralAssignmentToCsv(string serviceName, int tarefaId, string clientId)
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, "task_assignments.csv");
        var taskAssignments = new TaskAssignments
        {
            ClienteID = clientId,
            TarefaID = tarefaId,
            ServicoNome = serviceName
        };

        bool fileExists = File.Exists(filePath);
        using (var stream = File.Open(filePath, FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            if (!fileExists)
            {
                csv.WriteHeader<TaskAssignment>();
                csv.NextRecord();
            }

            csv.WriteRecord(taskAssignments);
            csv.NextRecord();
        }
    }
}
