export interface VocabularyEntry {
  id: number
  partOfSpeech?: string
  foreign: string
  transcription?: string
  audioUrl?: string
  examples: string[]
  translations: string[]
  synonyms: string[]
  antonyms: string[]
  createdAt: string
}

export interface CreateEntryRequest {
  partOfSpeech?: string
  foreign: string
  transcription?: string
  examples?: string[]
  translations: string[]
}

export interface PatchEntryRequest {
  partOfSpeech?: string
  foreign?: string
  transcription?: string
  examplesAdd?: string[]
  examplesRemove?: string[]
  translationsAdd?: string[]
  translationsRemove?: string[]
  synonymsAdd?: string[]
  synonymsRemove?: string[]
  antonymsAdd?: string[]
  antonymsRemove?: string[]
}
