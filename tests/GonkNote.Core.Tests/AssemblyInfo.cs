// xunit parallelisiert Testklassen standardmäßig. Das geht hier nicht: der Kern hat drei
// prozessweit statische Zustände, die sich zwei parallele Klassen gegenseitig umstellen
// würden — BlobStore.Current und ImageCache.Source (beide setzt der DatabaseService-
// Konstruktor) und der Bild-Cache in ImageCache selbst, der nicht threadsicher ist.
//
// Ein grüner Lauf wäre dann Glück, ein roter nicht reproduzierbar. Deshalb: seriell.
// Die Testmenge ist klein, das kostet Sekunden.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
