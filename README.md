# Transcriber
Клиент для работы в паре с VOSK-SERVER (https://github.com/alphacep/vosk-server).
Клиент захватывает аудио данные посредством WASAPI LOOPBACK или же из медиа файла. Медиа файл предварительно обрабатывается FFMPEG
В папке с программой должен находиться ffmpeg.exe (тестировалось с версией 2021-01-09).
Добавление языков осуществляется через прописывание в файл appsettings.json в раздел LanguageEndpointURIs : { "LANGUAGE" : "URL"}

![Alt text](https://github.com/Apheliont/Transcriber/blob/master/img/Screen01.png "Главная")