using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Net;
using System.Net.Mail;


string url = "https://w9.jujmanga.com";

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

CancellationToken cancellationToken = cancellationTokenSource.Token;

await Task.Run(() => Busca(url, cancellationToken), cancellationToken);
static async Task Busca(string url, CancellationToken cancellationToken)
{
    HttpClient client = new HttpClient();

    // faz a requisição GET
    HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();

    // extrai o conteúdo da resposta
    string responseBody = await response.Content.ReadAsStringAsync();

    // faz algo com o conteúdo, como analisar o HTML em busca de links para capítulos
    // e depois acessá-los usando o HttpClient

    // por exemplo, se o conteúdo contiver links para capítulos em tags <a>, você pode usar um parser HTML
    // como o HtmlAgilityPack para extrair os links e acessá-los um por um

    // exemplo de uso do HtmlAgilityPack para extrair links de tags <a>
    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
    htmlDoc.LoadHtml(responseBody);

    var links = htmlDoc.DocumentNode.Descendants("a")
        .Select(a => a.GetAttributeValue("href", null))
        .Where(href => !string.IsNullOrEmpty(href))
        .ToList();
    links.RemoveRange(1, 10);
    // percorre os links e acessa cada um deles
    foreach (string link in links)
    {
        if (link.Contains("jujutsu"))
        {
            HttpResponseMessage chapterResponse = await client.GetAsync(link);
            chapterResponse.EnsureSuccessStatusCode();

            string chapterResponseBody = await chapterResponse.Content.ReadAsStringAsync();

            // Cria uma instância do HtmlDocument e carrega o conteúdo HTML
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(chapterResponseBody);

            // Encontra todos os links de imagem na página
            List<string> imageLinks = new List<string>();
            HtmlNode div = document.DocumentNode.SelectSingleNode($"//div[@class='entry-inner']");
            Thread.Sleep(200);

            var imageSrcs = div.Descendants("img").Where(img => img.Attributes["src"]?.Value
            .Contains("r-world") == true)
            .Select(img => img.Attributes["src"].Value)
            .ToList();

            foreach (var src in imageSrcs)
            {
                imageLinks.Add(src);
            }

            // Baixa cada imagem e salva no disco
            int i = 1;
            foreach (string imageUrl in imageLinks)
            {
                // Faz a requisição da imagem
                response = await client.GetAsync(imageUrl);

                // Lê o conteúdo da imagem
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Salva a imagem no disco
                string imageName = $"imagem_{i}.jpg";
                string manga = "Jujutsu Kaisen";
                int startIndex = link.IndexOf("chapter-") + "chapter-".Length;
                int endIndex = link.IndexOf("/", startIndex);

                string number = link.Substring(startIndex, endIndex - startIndex);
                string nomePasta = manga + " " + "Chapter - " + number;
                string caminhoPasta = Path.Combine($"C:\\Users\\danie\\Downloads\\Mangas\\", manga + "\\" + nomePasta);

                Directory.CreateDirectory(caminhoPasta);
                string imagePath = Path.Combine(caminhoPasta, imageName);
                File.WriteAllBytes(imagePath, imageBytes);

                i++;
            }
        }
    }

    string rootFolder = @"C:\Users\danie\Downloads\Mangas\Jujutsu Kaisen";
    string[] subfolders = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
    foreach (string subfolder in subfolders)
    {
        string[] imageFiles = Directory.GetFiles(subfolder, "*.jpg");

        Array.Sort(imageFiles, new NumericComparer());

        if (imageFiles.Length > 0)
        {
            string substring = subfolder.Substring(subfolder.IndexOf("Jujutsu Kaisen\\") + "Jujutsu Kaisen\\".Length);
            string pdfFile = Path.Combine(subfolder, substring + ".pdf");
            using (FileStream fs = new FileStream(pdfFile, FileMode.Create))
            {
                using (Document doc = new Document())
                {
                    using (PdfWriter writer = PdfWriter.GetInstance(doc, fs))
                    {
                        doc.Open();
                        foreach (string imageFile in imageFiles)
                        {
                            using (FileStream imageStream = new FileStream(imageFile, FileMode.Open))
                            {
                                Image image = Image.GetInstance(imageStream);
                                doc.Add(image);
                            }
                        }
                        doc.Close();
                    }
                }
            }
        }
    }

    string pastaRaiz = @"C:\Users\danie\Downloads\Mangas\Jujutsu Kaisen";
    string[] subdiretorios = Directory.GetDirectories(pastaRaiz);

    int maxSizeInBytes = 20000000;
    Array.Sort(subdiretorios, new NumericComparer());

    foreach (string subdiretorio in subdiretorios)
    {
        string[] arquivos = Directory.GetFiles(subdiretorio, "*.pdf");

        MailMessage mensagem = new MailMessage();

        mensagem.From = new MailAddress("");

        mensagem.To.Add("");

        foreach (string arquivo in arquivos)
        {
            Attachment anexo = new Attachment(arquivo);
            FileInfo fileInfo = new FileInfo(arquivo);
            if (fileInfo.Length <= maxSizeInBytes)
            {
                mensagem.Attachments.Add(anexo);
            }
            else
            {
                Console.WriteLine(arquivo.ToString() + "Não foi enviado");
            }
        }

        // Crie um objeto SmtpClient para enviar a mensagem
        SmtpClient clienteSmtp = new SmtpClient("smtp-relay.sendinblue.com", 587);
        clienteSmtp.UseDefaultCredentials = false;
        clienteSmtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        clienteSmtp.Credentials = new NetworkCredential("gatinhogatinho1499@gmail.com", "7xQVZs4UHC35mfIr");
        clienteSmtp.EnableSsl = true;

        // Envie a mensagem
        clienteSmtp.Send(mensagem);

        // Limpe os anexos da mensagem
        foreach (Attachment anexo in mensagem.Attachments)
        {
            anexo.Dispose();
        }

        // Limpe a mensagem
        mensagem.Dispose();
    }
}
public class NumericComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        int xNum = ExtractNumber(x);
        int yNum = ExtractNumber(y);

        if (xNum == yNum)
        {
            return x.CompareTo(y);
        }

        return xNum.CompareTo(yNum);
    }

    private int ExtractNumber(string texto)
    {
        string numeros = string.Empty;
        foreach (char c in texto)
        {
            if (char.IsDigit(c))
            {
                numeros += c;
            }
        }
        return int.Parse(numeros);
    }
}
