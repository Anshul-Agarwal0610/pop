"use client"

import { useState, useRef } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { Image, Video, Music, FileText, Upload, X, SkipForward, Check } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type { MediaAttachment } from "@/lib/poll-creation"

interface MediaStepProps {
  value: MediaAttachment | null
  onChange: (value: MediaAttachment | null) => void
  onSkip: () => void
}

const MEDIA_TYPES = [
  { type: "image" as const, label: "Image", icon: Image, accept: "image/*", color: "text-pink-500 bg-pink-500/10" },
  { type: "video" as const, label: "Video", icon: Video, accept: "video/*", color: "text-purple-500 bg-purple-500/10" },
  { type: "audio" as const, label: "Audio", icon: Music, accept: "audio/*", color: "text-emerald-500 bg-emerald-500/10" },
  { type: "document" as const, label: "Document", icon: FileText, accept: ".pdf,.doc,.docx", color: "text-blue-500 bg-blue-500/10" },
]

export function MediaStep({ value, onChange, onSkip }: MediaStepProps) {
  const [selectedType, setSelectedType] = useState<typeof MEDIA_TYPES[number] | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file && selectedType) {
      const url = URL.createObjectURL(file)
      onChange({
        type: selectedType.type,
        url,
        name: file.name,
        size: file.size,
      })
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files?.[0]
    if (file && selectedType) {
      const url = URL.createObjectURL(file)
      onChange({
        type: selectedType.type,
        url,
        name: file.name,
        size: file.size,
      })
    }
  }

  const handleRemove = () => {
    onChange(null)
    setSelectedType(null)
  }

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="flex flex-col gap-6"
    >
      {/* Header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-pink-500/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <Upload className="h-8 w-8 text-pink-500" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          Attach media
        </h2>
        <p className="mt-2 text-muted-foreground">
          Make your poll more engaging (optional)
        </p>
      </div>

      <AnimatePresence mode="wait">
        {value ? (
          /* Preview */
          <motion.div
            key="preview"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="relative overflow-hidden rounded-2xl bg-card ring-2 ring-primary/20"
          >
            {value.type === "image" && (
              <img src={value.url} alt="" className="h-64 w-full object-cover" />
            )}
            {value.type === "video" && (
              <video src={value.url} className="h-64 w-full object-cover" controls />
            )}
            {value.type === "audio" && (
              <div className="flex h-32 items-center justify-center bg-muted p-6">
                <audio src={value.url} controls className="w-full" />
              </div>
            )}
            {value.type === "document" && (
              <div className="flex h-32 items-center justify-center gap-3 bg-muted p-6">
                <FileText className="h-12 w-12 text-blue-500" />
                <div>
                  <p className="font-medium text-foreground">{value.name}</p>
                  {value.size && (
                    <p className="text-sm text-muted-foreground">{formatFileSize(value.size)}</p>
                  )}
                </div>
              </div>
            )}
            <div className="flex items-center justify-between border-t border-border bg-card p-4">
              <div className="flex items-center gap-2">
                <Check className="h-5 w-5 text-emerald-500" />
                <span className="font-medium text-foreground">{value.name}</span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleRemove}
                className="text-destructive hover:bg-destructive/10 hover:text-destructive"
              >
                <X className="h-4 w-4 mr-1" />
                Remove
              </Button>
            </div>
          </motion.div>
        ) : selectedType ? (
          /* Upload Area */
          <motion.div
            key="upload"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
          >
            <input
              ref={fileInputRef}
              type="file"
              accept={selectedType.accept}
              onChange={handleFileSelect}
              className="hidden"
            />
            <div
              onClick={() => fileInputRef.current?.click()}
              onDragOver={(e) => { e.preventDefault(); setIsDragging(true) }}
              onDragLeave={() => setIsDragging(false)}
              onDrop={handleDrop}
              className={cn(
                "flex h-64 cursor-pointer flex-col items-center justify-center gap-4 rounded-2xl border-2 border-dashed transition-colors",
                isDragging
                  ? "border-primary bg-primary/5"
                  : "border-border bg-muted/30 hover:border-primary/50 hover:bg-muted/50"
              )}
            >
              <motion.div
                className={cn("rounded-2xl p-4", selectedType.color)}
                animate={{ scale: isDragging ? 1.1 : 1 }}
              >
                <selectedType.icon className="h-10 w-10" />
              </motion.div>
              <div className="text-center">
                <p className="font-medium text-foreground">
                  {isDragging ? "Drop your file here" : `Upload ${selectedType.label.toLowerCase()}`}
                </p>
                <p className="mt-1 text-sm text-muted-foreground">
                  or click to browse
                </p>
              </div>
            </div>
            <Button
              variant="ghost"
              onClick={() => setSelectedType(null)}
              className="mt-4 w-full text-muted-foreground"
            >
              Choose different type
            </Button>
          </motion.div>
        ) : (
          /* Type Selection */
          <motion.div
            key="select"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="grid grid-cols-2 gap-4"
          >
            {MEDIA_TYPES.map((type, index) => (
              <motion.button
                key={type.type}
                onClick={() => setSelectedType(type)}
                className="flex flex-col items-center gap-3 rounded-2xl bg-card p-6 ring-1 ring-border transition-all hover:ring-2 hover:ring-primary/50"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: index * 0.05 }}
                whileHover={{ scale: 1.02, y: -2 }}
                whileTap={{ scale: 0.98 }}
              >
                <div className={cn("rounded-xl p-3", type.color)}>
                  <type.icon className="h-7 w-7" />
                </div>
                <span className="font-medium text-foreground">{type.label}</span>
              </motion.button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Skip Button */}
      {!value && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3 }}
          className="flex justify-center"
        >
          <Button
            variant="ghost"
            onClick={onSkip}
            className="text-muted-foreground hover:text-foreground"
          >
            <SkipForward className="mr-2 h-4 w-4" />
            Skip this step
          </Button>
        </motion.div>
      )}
    </motion.div>
  )
}
