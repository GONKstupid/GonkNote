// Wie in GonkNote.Core.Tests seriell, und hier aus einem zweiten Grund: die Exportwege
// gehen über BlobStore.Current und ImageCache.Source. Beides ist prozessweit statisch und
// wird vom DatabaseService-Konstruktor gesetzt — zwei parallele Testklassen würden sich
// gegenseitig den Blob-Speicher unter den Füßen wegziehen.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
