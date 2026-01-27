# Konsolowy Blackjack

## O projekcie

Jest to prosty projekt realizujący połączenie sieciowe między serwerem a klientami za pośrednictwem protokołu TCP. Jest to aplikacja konsolowa, stąd niektóre rzeczy mogą wyglądać bardzo topornie. Uważam jednak, że sposób komunikacji klienta z serwerem umożliwia dodanie w przyszłości interfejsu graficznego, chociaż dla mnie na tym etapie była to bariera nie do przejścia :/.

## Serwer

Zaprojektowana struktura serwera dla tego projektu jest dosyć prosta i wykonuje podstawowe czynności. Przede wszystkim ciągle czeka na nowe prośby połączenia w sieci lokalnej na porcie 13000. Każdemu klientowi, z którym udało się nawiązać połączenie przydziela jego własny wątek, który dzieli strumień klienta TCP na strumienie wykorzystywane odpowiednio do odczytu i zapisu, co umożliwia jednoczesne pobieranie danych z serwera i wysyłanie mu poleceń. Wątek klienta cały czas czeka na ciąg znaków wysłany przez klienta zakończony znakiem nowej linii. Po otrzymaniu takiej wiadomości, próbuje ją przetłumaczyć na jedno z dostępnych poleceń i następnie wysyła odpowiedź.

Dodatkowo serwer ciągle sprawdza stan każdego z istniejących w grze stołów (co 100ms) i w razie potrzeby zapisuje dane zarejestrowanych użytkowników do lokalnego pliku bazy danych oraz wysyła do każdego "_zainteresowanego_" gracza aktualny stan stołu.

## Klient

W momencie włączenia aplikacji użytkownika (klienta) dochodzi do próby połączenia się z serwerem. Jeśli próba jest nieudana (np. w przypadku, kiedy serwer jest nieosiągalny lub nie został włączony), odpowiedni komunikat jest wypisywany w odpowiednim miejscu na ekranie użytkownika. Użytkownik w trakcie _grania_ może się znajdować w jednej z trzech "_lokalizacji_":

- ekran powitalny - tutaj się znajduje każdy niezalogowany użytkownik. Nie ma tutaj dostępu do żadnych zasobów gry, oprócz kilku poleceń umożliwiających uwierzytelnianie. Dostępne polecenia w tej sekcji to:
    - **login** - zalogowanie się na konto użytkownika. Użytkownik musi być zarejestrowany. Dane zalogowanych użytkowników są zapisywane do bazy danych i przywracane w momencie ponownego logowania;
    - **register** - umożliwia rejestrację nowego użytkownika o podanym loginie i haśle.;
    - **guest** - zalogowanie się jako anonimowy użytkownik o loginie guest_XXX, gdzie XXX to losowa liczba trzycyfrowa. Dane gości nie są zapisywane do bazy danych;
    - **exit** - wyjście z gry;

- lobby - wyświetla się tutaj lista wszystkich dostępnych stołów do gry. Użytkownik może dołączyć do dowolnego stołu, nawet jeśli stół jest przepełniony. Wówczas może być wyłącznie obserwatorem gry. Dostępne polecenia:
    - **list** - wypisuje listę wszystkich dostępnych stołów (lista stołów jest automatycznie wysyłana przez serwer w momencie aktualizacji stanu dowolnego z istniejących stołów, więc to polecenie na danym etapie jest mało użyteczne);
    - **join** - dołącza do stołu po numerze _id_ lub po jego nazwie. W momencie niejednoznaczności wyboru (np. stół o numerze id 2 i stół o nazwie "2"), gracz zostaje dołączony do stołu który jako pierwszy został dopasowany;
    - **create** - tworzy nowy stół o danej nazwie i o danej maksymalnej liczbie graczy.
    - **logout** - wylogowywuje gracza z jego konta lub konta gościa;
    - **exit** - wychodzi z gry;

- stół - obiektywnie najciekawsza lokalizacja w grze. Przy stole znajdują się krzesła, na których mogą usiąść gracze, aby zacząć grę. Gra się zaczyna po ustalonym odliczaniu. Długość czasu oczekiwania na rozgrywkę zależy od tego ile graczy aktualnie zrobiło zakład. W momencie przyjęcia przez serwer pierwszego zakładu, rozpoczyna się odliczanie trwające 30 sekund. Jeśli każdy gracz siedzący przy stole zrobił swój zakład, czas oczekiwania na rundę jest ustawiany na 10 sekund. Po upływie czasu, diler rozdaje karty. W kolejności od najmniejszego identyfikatora miejsca do największego, gracze wykonują ruchy. Na wykonanie każdego ruchu gracz ma 30 sekund, a po upływie tego czasu kolejka przechodzi do następnego gracza. Zasady ustalania zwycięzców są zgodne z klasycznymi zasadami gry "blackjack" (nie są jedynie zaimplementowane opcje _insurance_, _double_ i _split_). Dostępne polecenia:
    - **sit** - umożliwia dołączenie do gry. Pozwala graczowi usiąść na wybranym miejscu o ile jest ono wolne i jego numer nie przekracza maksymalnej liczby graczy ustalonej dla danego stołu;
    - **back** - zwolnienie miejsca przy stole. Wówczas gracz zostaje obserwatorem danego stołu, czyli nie wraca do lobby;
    - **leave** - opuszcza dany stół. Gracz wraca do lobby. Jeśli gracz przed wykonaniem tego polecenia siedział przy stole, automatycznie zwalnia swoje miejsce;
    - **bet** - robi zakład. Aby wykonać to polecenie, gracz musi siedzieć przy stole;
    - **hit** - dobiera kolejną kartę do ręki. Gracz musi siedzieć przy stole oraz być w trakcie rozgrywki;
    - **stand** - przekazanie ruchu kolejnemu graczowi, wstrzymanie się przed dobieraniem kolejnej karty. Zasady użycia podobne jak dla polecenia **hit**;
    - **exit** - wychodzi z gry;

## Known issues

1. Zapewne można byłoby czekać na nowe połączenia asynchronicznie, aby w przypadku, kiedy nawiązanie połączenia z użytkownikiem A jest utrudnione, użytkownik B może dołączyć bez problemu;
2. Serwer w swoich wiadomościach aktualizujących stan stołu często wysyła zbędne informacje. W momencie aktualizacji listy stołów, zostaje wysłana np. lista kart w ręce dilera, a w wiadomości aktualizacji stołu dane o kartach dilera są wysyłane w taki sposób, że przechwycenie tej wiadomości umożliwi odczytanie ukrytej karty w jego ręce przy pomocy pól Rank i Suit.
3. Zgaduję, że dobrą praktyką byłoby przekazanie do nieskończonych pętli CancellationTokenu, co umożliwiłoby poprawniejsze wyjście z programu.
