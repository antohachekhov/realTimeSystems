﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using static PortChat;

public class PortChat
{
    static bool _continue;
    static SerialPort _serialPort;

    // typeProtocol: 0 - bin, 1 - string
    static bool typeProtocol = false;
    static string encoding = "ASCII";
    static CheckSumAlg checkSumAlg = CheckSumAlg.Not;

    public enum CheckSumAlg
    {
        Not = 0,
        Simple = 1,
        LRC = 2,
        CRC16 = 3,
        CRC32 = 4
    }


    public static void Main()
    {

        // Создание объекта последовательного порта с настройками по умолчанию 
        _serialPort = new SerialPort();

        // PortName - наименование последовательного порта
        SetPortName(ref _serialPort);

        // BaudRate - Возвращает или задает скорость передачи для последовательного порта (бит в секунду).
        SetPortBaudRate(ref _serialPort);

        // Parity - протокол контроля четности
        // Отвечается за контроль чертности и паритет четности
        // Контроль четности: Even(четное) и Odd(нечетное)
        // Паритет четности: Mark(бит четности = 1) и Space(бит четности = 0)
        SetPortParity(ref _serialPort);

        // Устанавливаем таймаут для чтения и записи
        _serialPort.ReadTimeout = 50000;
        _serialPort.WriteTimeout = 50000;

        SetTypeProtocol();
        SetCheckSumAlg();
        SetEncoding();

        // Подписка на обработчик получения сообщений
        _serialPort.DataReceived += GetMessage;
        // Открывает соединение последовательного порта
        _serialPort.Open();
        _continue = true;


        string message;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Console.WriteLine("Type QUIT to exit");
        while (_continue)
        {
            message = Console.ReadLine();

            if (stringComparer.Equals("QUIT", message))
            {
                _continue = false;
            }
            else
            {
                SendMessage(message);
            }
        }

        _serialPort.Close();
    }

    // Отображдает существуюшие порты и задает выбранный порт
    public static void SetPortName(ref SerialPort _serialPort)
    {
        string newPortName;

        Console.WriteLine("Существующие порты:");
        foreach (string s in SerialPort.GetPortNames())
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Введите название COM порта (По умолчанию: {0}): ", _serialPort.PortName);
        newPortName = Console.ReadLine();

        if (!(newPortName == "") && newPortName.ToLower().StartsWith("com"))
        {
            _serialPort.PortName = newPortName;
        }
    }



    // Задает скорость передачи для последовательного порта.
    public static void SetPortBaudRate(ref SerialPort _serialPort)
    {
        string newBaudRate;

        Console.Write("Скорость передачи (По умолчанию: {0}): ", _serialPort.BaudRate);
        newBaudRate = Console.ReadLine();

        if (newBaudRate != "")
        {
            _serialPort.BaudRate = int.Parse(newBaudRate);
        }
    }



    // Отображает существующие протоколы контроля четности и задает выбранный протокол
    public static void SetPortParity(ref SerialPort _serialPort)
    {
        string newParity;

        Console.WriteLine("Существующие протоколы контроля четности:");
        foreach (string s in Enum.GetNames(typeof(Parity)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Протокол контроля четности (По умолчанию: {0}):", _serialPort.Parity.ToString());
        newParity = Console.ReadLine();

        if (newParity != "")
        {
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), newParity, true);
        }
    }

    public static void SetTypeProtocol()
    {
        Console.WriteLine("Возможные типы протоколо передачи данных:");
        Console.WriteLine("   BIN");
        Console.WriteLine("   STR");
        Console.Write("Протокол передачи данных (По умолчанию: BIN): ");
        string inputType = Console.ReadLine();
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

        if (inputType != "")
        {
            typeProtocol = !stringComparer.Equals("BIN", inputType);
        }
    }

    public static void SetCheckSumAlg()
    {
        Console.WriteLine("Существующие функции контрольной суммы:");
        foreach (string s in Enum.GetNames(typeof(CheckSumAlg)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Функция контрольной суммы (По умолчанию: Not): ");
        string inputCheckSumAlg = Console.ReadLine();

        if (inputCheckSumAlg != "")
        {
            checkSumAlg = (CheckSumAlg)Enum.Parse(typeof(CheckSumAlg), inputCheckSumAlg, true);
        }
    }

    public static void SetEncoding()
    {
        Console.WriteLine("Существующие кодировки:");
        Console.WriteLine("   ASCII");
        Console.WriteLine("   UTF-8");

        Console.Write("Кодировка (По умолчанию: ASCII): ");
        string inputEncoding = Console.ReadLine();
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

        if (inputEncoding != "")
        {
            if (stringComparer.Equals("UTF-8", inputEncoding))
            {
                _serialPort.Encoding = Encoding.UTF8;
            }
        }
    }

    public static byte CalcSimpleCheckSum(byte[] messageBytes)
    {
        byte checkSum = 0;
        foreach (byte b in messageBytes)
        {
            checkSum ^= b;
        }
        return checkSum;
    }

    public static byte CalcLongRedCheck(byte[] messageBytes)
    {
        byte checkSum = 0;

        for (int i = 0; i < messageBytes.Length; i++)
        {
            checkSum = (byte)((checkSum + messageBytes[i]) % 255);
        }

        checkSum = (byte)(255 - checkSum);
        checkSum = (byte)((checkSum + 1) % 255);

        return checkSum;

    }


    //public static ushort CalcCRC16Sasha(byte[] messageBytes)
    //{
    //    // задаем регистр CRC
    //    ushort CRC = 0xFFFF;

    //    for (int i = 0; i < messageBytes.Length; i++)
    //    {
    //        // Первый байт сообщения складывается по исключающему ИЛИ с содер жимым регистра CRC
    //        CRC = (ushort)((CRC & 0xFF00) + (messageBytes[i] ^ CRC));

    //        for (int j = 0; j < 8; j++)
    //        {

    //            if ((CRC & 0x0001) != 0)
    //            {
    //                CRC = (ushort)((CRC >> 1) ^ (0xA001));
    //            }
    //            else
    //                CRC >>= 1;
    //        }
    //    }

    //    return CRC;
    //}
    public static ushort CalcCRC16Ira(byte[] messageBytes)
    {
        // Задаем контрольную сумму из 2 байт 
        ushort checkSum = 0;

        // Начинаем обрабатывать сообщение побайтно
        for (int i = 0; i < messageBytes.Length; i++)
        {
            checkSum ^= (ushort)(messageBytes[i] << 8); // сдвиг вправо

            for (int j = 0; j < 8; j++)
            {
                if ((ushort)(checkSum & (ushort)0x8000u) != 0) // если младший бит не равен 0, 32768 = 1000 0000 0000 0000
                    checkSum = (ushort)((ushort)(checkSum << 1) ^ (ushort)0x1021u); // исключающее или регистра, 4129 = 0001 0000 0010 0001
                else // если нет
                    checkSum <<= 1; // то сдвиг
            }
        }
        return checkSum;
    }

    public static uint CalcCRC32(byte[] messageBytes)
    {
        var crcTable = new uint[256];
        uint checkSum;
        for (uint i = 0; i < 256; i++)
        {
            checkSum = i;
            for (uint j = 0; j < 8; j++)
                checkSum = (checkSum & 1) != 0 ? (checkSum >> 1) ^ 0xEDB88320 : checkSum >> 1;
            crcTable[i] = checkSum;
        }
        checkSum = messageBytes.Aggregate(0xFFFFFFFF, (current, s) => crcTable[(current ^ s) & 0xFF] ^ (current >> 8));
        checkSum ^= 0xFFFFFFFF;
        return checkSum;
    }

    // Получение сообщений
    private static void GetMessage(object sender, SerialDataReceivedEventArgs e)
    {
        // Чтение полученных байтов
        var receivedMessage = new byte[_serialPort.BytesToRead];
        _serialPort.Read(receivedMessage, 0, _serialPort.BytesToRead);

        // Проверка контрольной суммы
        if (CheckControlSum(receivedMessage)) ;
        WriteMessageToConsole(receivedMessage);// Вывод сообщения на экран
    }


    private static void WriteMessageToConsole(byte[] messageBytes)
    {
        switch (checkSumAlg)
        {
            case CheckSumAlg.Not:
                break;
            case CheckSumAlg.Simple:
                {

                    // Вычиселние сообщения без контрольной суммы
                    Array.Resize(ref messageBytes, messageBytes.Length - 1);
                    break;
                }
            case CheckSumAlg.LRC:
                {
                    // Вычиселние сообщения без контрольной суммы
                    Array.Resize(ref messageBytes, messageBytes.Length - 1);
                    break;
                }
            case CheckSumAlg.CRC16:
                {

                    // Вычиселние сообщения без контрольной суммы
                    Array.Resize(ref messageBytes, messageBytes.Length - 2);
                    break;
                }
            case CheckSumAlg.CRC32:
                {

                    // Вычиселние сообщения без контрольной суммы
                    Array.Resize(ref messageBytes, messageBytes.Length - 4);
                    break;
                }
        }


        // Вывод сообщения на экран
        Console.WriteLine("Количество байт в полученном сообщении: " + messageBytes.Length.ToString());
        if (!typeProtocol)
        {
            Console.WriteLine("Полученное сообщение: " + BitConverter.ToDouble(messageBytes, 0).ToString());
        }
        else
        {
            Console.WriteLine("Полученное сообщение: " + _serialPort.Encoding.GetString(messageBytes));
        }

        return;
    }


    private static bool CheckControlSum(byte[] messageBytes)
    {
        bool flagCheckSumEqual = false;
        switch (checkSumAlg)
        {
            case CheckSumAlg.Not:
                return true;
            case CheckSumAlg.Simple:
                {

                    // Последний байт в сообщении - контрольная сумма
                    byte checkSumInMessage = messageBytes[messageBytes.Length - 1];

                    // Вычиселние сообщения без контрольной суммы
                    byte[] array = new byte[messageBytes.Length];
                    messageBytes.CopyTo(array, 0);

                    Array.Resize(ref array, array.Length - 1);
                    byte[] checkSum = new byte[] { CalcSimpleCheckSum(array) };
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{BitConverter.ToString(checkSum)}");
                    // Сравнение двух сумм
                    if (checkSum[0] == checkSumInMessage)
                    {
                        flagCheckSumEqual = true;
                    }
                    break;
                }
            case CheckSumAlg.LRC:
                {
                    // Последний байт в сообщении - контрольная сумма
                    byte checkSumInMessage = messageBytes[messageBytes.Length - 1];

                    // Вычиселние сообщения без контрольной суммы
                    byte[] array = new byte[messageBytes.Length];
                    messageBytes.CopyTo(array, 0);

                    Array.Resize(ref array, array.Length - 1);
                    byte[] checkSum = new byte[] { CalcLongRedCheck(array) };
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{BitConverter.ToString(checkSum)}");
                    // Сравнение двух сумм
                    if (checkSum[0] == checkSumInMessage)
                    {
                        flagCheckSumEqual = true;
                    }
                    break;
                }
            case CheckSumAlg.CRC16:
                {
                    // Последние 2 байта в сообщении - контрольная сумма
                    byte[] checkSumInMessage = new byte[2];

                    for (int i = 0; i < checkSumInMessage.Length; i++)
                    {
                        checkSumInMessage[i] = messageBytes[messageBytes.Length - checkSumInMessage.Length + i];
                    }

                    // Вычиселние сообщения без контрольной суммы
                    byte[] array = new byte[messageBytes.Length];
                    messageBytes.CopyTo(array, 0);
                    Array.Resize(ref array, array.Length - 2);

                    //byte[] checkSum = BitConverter.GetBytes(CalcCRC16Sasha(array));
                    //Console.WriteLine("Контрольная сумма посылки-Саша: " + $"0x{BitConverter.ToString(checkSum)}");

                    byte[] checkSum = BitConverter.GetBytes(CalcCRC16Ira(array));
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{BitConverter.ToString(checkSum)}");

                    if (checkSum.Equals(checkSumInMessage))
                    {
                        flagCheckSumEqual = true;
                    }

                    break;
                }
            case CheckSumAlg.CRC32:
                {

                    // Последние 2 байта в сообщении - контрольная сумма
                    byte[] checkSumInMessage = new byte[4];

                    for (int i = 0; i < checkSumInMessage.Length; i++)
                    {
                        checkSumInMessage[i] = messageBytes[messageBytes.Length - checkSumInMessage.Length + i];
                    }

                    // Вычиселние сообщения без контрольной суммы
                    byte[] array = new byte[messageBytes.Length];
                    messageBytes.CopyTo(array, 0);
                    Array.Resize(ref array, array.Length - 4);

                    //byte[] checkSum = BitConverter.GetBytes(CalcCRC16Sasha(array));
                    //Console.WriteLine("Контрольная сумма посылки-Саша: " + $"0x{BitConverter.ToString(checkSum)}");

                    byte[] checkSum = BitConverter.GetBytes(CalcCRC32(array));
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{BitConverter.ToString(checkSum)}");

                    if (checkSum.Equals(checkSumInMessage))
                    {
                        flagCheckSumEqual = true;
                    }
                    break;
                }
        }

        return flagCheckSumEqual;
    }


    // Вычисление контрольной суммы
    private static byte[] CalcCheckSum(byte[] messageBytes)
    {
        byte[] checkSum;

        switch (checkSumAlg)
        {
            case CheckSumAlg.Not:
                return new byte[0];
            case CheckSumAlg.Simple:
                {
                    //контрольная сумма состоит из 1 байта
                    checkSum = new byte[1];
                    checkSum[0] = CalcSimpleCheckSum(messageBytes);
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{ BitConverter.ToString(checkSum)}");
                    return checkSum;
                }
            case CheckSumAlg.LRC:
                {
                    // контрольная сумма состоит из 1 байта
                    checkSum = new byte[1];
                    checkSum[0] = CalcLongRedCheck(messageBytes);
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{  BitConverter.ToString(checkSum)}");
                    return checkSum;
                }
            case CheckSumAlg.CRC16:
                {

                    checkSum = BitConverter.GetBytes(CalcCRC16Ira(messageBytes));
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{ BitConverter.ToString(checkSum)}");
                    return checkSum;
                }
            case CheckSumAlg.CRC32:
                {
                    checkSum = BitConverter.GetBytes(CalcCRC32(messageBytes));
                    Console.WriteLine("Контрольная сумма посылки: " + $"0x{  BitConverter.ToString(checkSum)}");
                    return checkSum;
                }
            default:
                throw new Exception("Выбран нереализованный алгоритм подсчета контрольной суммы");
        }
    }


    // Отправка сообщения
    private static void SendMessage(string message)
    {

        byte[] messageBytes;
        // Если формат данных бинарный
        if (!typeProtocol)
            try
            {
                messageBytes = BitConverter.GetBytes(Convert.ToDouble(message));
            }
            catch (FormatException)
            {
                throw new Exception("В биннарном формате передаются только числа");
            }
        else
            messageBytes = _serialPort.Encoding.GetBytes(message);

        Console.WriteLine("Количество байт в отправленном сообщении: " + messageBytes.Length);
        // Добавление контрольной суммы
        // Переведем массив байт в список, чтобы было проще добавить байты контрольной суммы
        List<byte> messageListByte = messageBytes.ToList();

        byte[] checkSum = CalcCheckSum(messageBytes);

        for (int i = 0; i < checkSum.Length; i++)
        {
            messageListByte.Add(checkSum[i]);
        }


        byte[] newMessageBytes = messageListByte.ToArray();

        // Отправка сообщения
        _serialPort.Write(newMessageBytes, 0, newMessageBytes.Length);
    }



}