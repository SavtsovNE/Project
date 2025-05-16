using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DomainGenerator
{
    // Список распространенных TLD для вариаций
    private static readonly List<string> CommonTlds = new List<string>
    {
        "com", "org", "net", "info", "biz", "us", "co.uk", "de", "ru", "cn",
        "online", "site", "store", "xyz", "io", "mobi", "cc", "tv"
    };

    // Список распространенных префиксов для фишинга
    private static readonly List<string> CommonPrefixes = new List<string>
    {
        "login-", "secure-", "verify-", "account-", "my-", "web-", "signin-", "auth-"
    };

    // Карта простых замен символов (ASCII homoglyphs)
    private static readonly Dictionary<char, char> CharacterReplacements = new Dictionary<char, char>
    {
        {'o', '0'}, {'O', '0'},
        {'l', '1'}, {'I', '1'}, {'i', '1'},
        {'s', '5'}, {'S', '5'},
        {'a', '4'}, {'A', '4'},
        {'e', '3'}, {'E', '3'},
        {'t', '7'}, {'T', '7'}
        // Можно добавить больше, но нужно быть осторожным с количеством комбинаций
    };

    public static void Main(string[] args)
    {
        Console.WriteLine("Генератор псевдофишинговых доменов");

        // Определяем путь к файлу на рабочем столе
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string protectedDomainsFileName = "protected_domains.txt";
        string protectedDomainsFilePath = Path.Combine(desktopPath, protectedDomainsFileName);

        string outputDomainsFileName = "generated_phishing_domains.txt";
        // Устанавливаем путь для выходного файла на рабочий стол
        string outputDomainsFilePath = Path.Combine(desktopPath, outputDomainsFileName);


        Console.WriteLine($"Ожидаю файл с защищенными доменами: {protectedDomainsFilePath}");
        Console.WriteLine($"Результат будет сохранен в файл: {outputDomainsFilePath}");
        Console.WriteLine("-------------------------------------------------");

        List<string> protectedDomains = new List<string>();

        // Чтение защищенных доменов из файла
        try
        {
            if (File.Exists(protectedDomainsFilePath))
            {
                protectedDomains = File.ReadAllLines(protectedDomainsFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line)) // Пропускаем пустые строки
                                       .Select(line => line.Trim().ToLower()) // Удаляем пробелы и приводим к нижнему регистру
                                       .ToList();
                Console.WriteLine($"Прочитано {protectedDomains.Count} защищенных доменов.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: Файл '{protectedDomainsFileName}' не найден на рабочем столе.");
                Console.ResetColor();
                Console.WriteLine("Пожалуйста, создайте его и заполните список доменов.");
                Console.WriteLine("Нажмите любую клавишу для выхода.");
                Console.ReadKey();
                return;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Произошла ошибка при чтении файла: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Нажмите любую клавишу для выхода.");
            Console.ReadKey();
            return;
        }

        if (protectedDomains.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("В файле защищенных доменов нет записей.");
            Console.ResetColor();
            Console.WriteLine("Нажмите любую клавишу для выхода.");
            Console.ReadKey();
            return;
        }

        // Генерация фишинговых доменов
        HashSet<string> generatedDomains = new HashSet<string>(); // Используем HashSet для автоматической уникальности

        Console.WriteLine("Начинается генерация псевдофишинговых доменов...");

        foreach (string legitDomain in protectedDomains)
        {
            Console.WriteLine($"Обработка: {legitDomain}");

            // Парсим домен на основную часть и TLD
            if (TryParseDomain(legitDomain, out string mainPart, out string tld))
            {
                // Добавляем вариации на основе основной части домена
                AddTypos(mainPart, tld, generatedDomains, CommonTlds);
                AddHyphenations(mainPart, tld, generatedDomains, CommonTlds);
                AddReplacements(mainPart, tld, generatedDomains, CommonTlds);

                // Добавляем префиксы к оригинальному домену
                AddPrefixes(legitDomain, generatedDomains);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Предупреждение: Не удалось разобрать домен '{legitDomain}'. Пропускаем.");
                Console.ResetColor();
            }
        }

        Console.WriteLine($"Генерация завершена. Сгенерировано {generatedDomains.Count} уникальных доменов.");

        // Сохранение сгенерированных доменов в файл
        try
        {
            File.WriteAllLines(outputDomainsFilePath, generatedDomains);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Сгенерированные домены успешно сохранены в файл: {outputDomainsFilePath}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Произошла ошибка при записи файла: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine("Нажмите любую клавишу для выхода.");
        Console.ReadKey();
    }

    /// <summary>
    /// Пытается разобрать домен на основную часть и TLD.
    /// Обрабатывает домены вида domain.tld и sub.domain.tld, а также www.domain.tld.
    /// </summary>
    private static bool TryParseDomain(string domain, out string mainPart, out string tld)
    {
        mainPart = null;
        tld = null;

        if (string.IsNullOrWhiteSpace(domain)) return false;

        string[] parts = domain.Split('.');
        if (parts.Length < 2) return false; // Минимум domain.tld

        tld = parts.Last();

        // Ищем основную часть: это часть перед последней точкой, исключая потенциальный поддомен (например, www)
        // Просто берем предпоследнюю часть как основную для простоты
        // Более сложный парсинг потребовал бы списка известных TLD и правил
        if (parts.Length >= 2)
        {
            mainPart = parts[parts.Length - 2];
            return true;
        }

        return false;
    }


    /// <summary>
    /// Генерирует вариации домена с опечатками (пропуск, вставка, замена, удвоение).
    /// </summary>
    private static void AddTypos(string mainPart, string originalTld, HashSet<string> generatedDomains, List<string> commonTlds)
    {
        if (string.IsNullOrWhiteSpace(mainPart)) return;

        List<string> typoVariations = new List<string>();

        // Пропуск символа
        for (int i = 0; i < mainPart.Length; i++)
        {
            // Убедимся, что не создаем пустую строку, если домен был из одного символа
            if (mainPart.Length > 1)
            {
                typoVariations.Add(mainPart.Remove(i, 1));
            }
        }

        // Замена соседних символов (простая перестановка)
        for (int i = 0; i < mainPart.Length - 1; i++)
        {
            char[] chars = mainPart.ToCharArray();
            char temp = chars[i];
            chars[i] = chars[i + 1];
            chars[i + 1] = temp;
            typoVariations.Add(new string(chars));
        }

        // Удвоение символов (если уже есть повторяющиеся)
        for (int i = 0; i < mainPart.Length - 1; i++)
        {
            if (mainPart[i] == mainPart[i + 1])
            {
                typoVariations.Add(mainPart.Insert(i + 1, mainPart[i].ToString()));
            }
        }
        // Удвоение каждого символа по отдельности (может быть много вариаций)
        for (int i = 0; i < mainPart.Length; i++)
        {
            typoVariations.Add(mainPart.Insert(i, mainPart[i].ToString()));
        }


        // Вставка распространенных символов (ограниченный набор, чтобы не генерировать слишком много)
        char[] commonCharsToInsert = { 'a', 'e', 'i', 'o', 'u', 's', 'l', 'n', 'r', 'd', 't' };
        for (int i = 0; i <= mainPart.Length; i++) // Вставляем на каждую позицию, включая конец
        {
            foreach (char charToInsert in commonCharsToInsert)
            {
                typoVariations.Add(mainPart.Insert(i, charToInsert.ToString()));
            }
        }


        // Комбинируем с оригинальным TLD и другими TLD
        foreach (string typoPart in typoVariations.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            generatedDomains.Add($"{typoPart}.{originalTld}"); // С оригинальным TLD
            foreach (string tld in commonTlds) // С другими TLD
            {
                if (!string.Equals(tld, originalTld, StringComparison.OrdinalIgnoreCase))
                {
                    generatedDomains.Add($"{typoPart}.{tld}");
                }
            }
        }
    }

    /// <summary>
    /// Генерирует вариации домена с добавлением распространенных фишинговых префиксов.
    /// Применяется к оригинальному домену.
    /// </summary>
    private static void AddPrefixes(string legitDomain, HashSet<string> generatedDomains)
    {
        if (string.IsNullOrWhiteSpace(legitDomain)) return;

        foreach (string prefix in CommonPrefixes)
        {
            generatedDomains.Add($"{prefix}{legitDomain}");
        }
    }

    /// <summary>
    /// Генерирует вариации домена с использованием других распространенных TLD.
    /// Применяется к основной части домена.
    /// </summary>
    // Этот метод не используется напрямую в Main, но оставлен как пример
    private static void AddTldVariations(string mainPart, List<string> commonTlds, HashSet<string> generatedDomains)
    {
        if (string.IsNullOrWhiteSpace(mainPart)) return;

        foreach (string tld in commonTlds)
        {
            generatedDomains.Add($"{mainPart}.{tld}");
        }
    }

    /// <summary>
    /// Генерирует вариации домена с вставкой дефисов.
    /// </summary>
    private static void AddHyphenations(string mainPart, string originalTld, HashSet<string> generatedDomains, List<string> commonTlds)
    {
        if (string.IsNullOrWhiteSpace(mainPart) || mainPart.Length <= 1) return;

        List<string> hyphenVariations = new List<string>();

        // Вставляем дефис на каждую возможную позицию
        for (int i = 1; i < mainPart.Length; i++) // Дефис не может быть в начале или конце
        {
            string hyphenatedPart = mainPart.Insert(i, "-");
            hyphenVariations.Add(hyphenatedPart);
            // Можно добавить вариации с несколькими дефисами, но это быстро увеличивает количество
        }

        // Комбинируем с TLD
        foreach (string hyphenPart in hyphenVariations.Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            generatedDomains.Add($"{hyphenPart}.{originalTld}"); // С оригинальным TLD
            foreach (string tld in commonTlds) // С другими TLD
            {
                if (!string.Equals(tld, originalTld, StringComparison.OrdinalIgnoreCase))
                {
                    generatedDomains.Add($"{hyphenPart}.{tld}");
                }
            }
        }
    }

    /// <summary>
    /// Генерирует вариации домена с заменой символов на визуально похожие (ASCII).
    /// Генерирует вариации с ОДНОЙ заменой символа.
    /// </summary>
    private static void AddReplacements(string mainPart, string originalTld, HashSet<string> generatedDomains, List<string> commonTlds)
    {
        if (string.IsNullOrWhiteSpace(mainPart)) return;

        List<string> replacementVariations = new List<string>();

        // Генерируем вариации с одной заменой символа
        for (int i = 0; i < mainPart.Length; i++)
        {
            char originalChar = mainPart[i];
            // Проверяем, есть ли замена для символа
            if (CharacterReplacements.TryGetValue(originalChar, out char replacementChar))
            {
                // Создаем новую строку с заменой символа на текущей позиции
                char[] chars = mainPart.ToCharArray();
                chars[i] = replacementChar;
                replacementVariations.Add(new string(chars));
            }
        }

        // Комбинируем с TLD
        foreach (string replacementPart in replacementVariations.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            generatedDomains.Add($"{replacementPart}.{originalTld}"); // С оригинальным TLD
            foreach (string tld in commonTlds) // С другими TLD
            {
                if (!string.Equals(tld, originalTld, StringComparison.OrdinalIgnoreCase))
                {
                    generatedDomains.Add($"{replacementPart}.{tld}");
                }
            }
        }
    }
}
